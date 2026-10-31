using System.Net;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

public class ClientTests
{
    private static readonly Guid User = Guid.NewGuid();

    private const string ActivityJson =
        """{"id":55,"name":"Morning Run","sport_type":"TrailRun","start_date":"2026-10-17T09:00:00Z","elapsed_time":1800}""";

    private const string PlaysJson = """
        {"items":[
          {"track":{"id":"t1","name":"Song One","duration_ms":240000,"artists":[{"name":"A"},{"name":"B"}],
                    "album":{"images":[{"url":"https://img/1.jpg"},{"url":"https://img/2.jpg"}]}},
           "played_at":"2026-10-17T09:10:00.000Z"},
          {"track":{"id":"t2","name":"Song Two","duration_ms":180000,"artists":[{"name":"C"}],"album":{"images":[]}},
           "played_at":"2026-10-17T09:05:00.000Z"}
        ]}
        """;

    [Fact]
    public async Task Strava_client_reads_the_activity_with_a_bearer_token()
    {
        var handler = new FakeHttpMessageHandler().Reply(ActivityJson);
        var tokens = new StubTokenProvider();
        var client = new StravaClient(new HttpClient(handler), tokens);

        var activity = await client.GetActivityAsync(User, 55);

        Assert.Equal([User], tokens.Users);
        Assert.Equal("Morning Run", activity.Name);
        Assert.Equal("TrailRun", activity.SportType);
        Assert.Equal(new DateTimeOffset(2026, 10, 17, 9, 0, 0, TimeSpan.Zero), activity.StartDate);
        Assert.Equal(1800, activity.ElapsedTimeSeconds);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://www.strava.com/api/v3/activities/55", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("stub-token", request.Headers.Authorization.Parameter);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task Strava_client_reads_the_description_and_treats_a_missing_or_null_one_as_none()
    {
        var handler = new FakeHttpMessageHandler()
            .Reply("""{"name":"A","type":"Run","start_date":"2026-10-17T09:00:00Z","elapsed_time":60,"description":"Felt great"}""")
            .Reply("""{"name":"A","type":"Run","start_date":"2026-10-17T09:00:00Z","elapsed_time":60,"description":null}""")
            .Reply("""{"name":"A","type":"Run","start_date":"2026-10-17T09:00:00Z","elapsed_time":60}""");
        var client = new StravaClient(new HttpClient(handler), new StubTokenProvider());

        Assert.Equal("Felt great", (await client.GetActivityAsync(User, 1)).Description);
        Assert.Null((await client.GetActivityAsync(User, 1)).Description);
        Assert.Null((await client.GetActivityAsync(User, 1)).Description);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task Strava_client_puts_the_description_to_the_activity_with_a_bearer_token()
    {
        var handler = new FakeHttpMessageHandler().Reply("{}");
        var client = new StravaClient(new HttpClient(handler), new StubTokenProvider());

        await client.UpdateDescriptionAsync(User, 55, "Line one\nLine two");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("https://www.strava.com/api/v3/activities/55", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("""{"description":"Line one\nLine two"}""", Assert.Single(handler.RequestBodies));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task Strava_client_maps_a_refused_description_update_to_upstream()
    {
        var handler = new FakeHttpMessageHandler().Reply("{}", HttpStatusCode.Forbidden);
        var client = new StravaClient(new HttpClient(handler), new StubTokenProvider());

        await Assert.ThrowsAsync<UpstreamServiceException>(() => client.UpdateDescriptionAsync(User, 55, "x"));
    }

    [Fact]
    public async Task Strava_client_falls_back_to_the_older_type_field()
    {
        var handler = new FakeHttpMessageHandler().Reply(
            """{"name":"Old","type":"Run","start_date":"2026-10-17T09:00:00Z","elapsed_time":60}""");
        var client = new StravaClient(new HttpClient(handler), new StubTokenProvider());

        Assert.Equal("Run", (await client.GetActivityAsync(User, 1)).SportType);
    }

    [Fact]
    public async Task Strava_client_maps_a_missing_activity_to_not_found_and_other_failures_to_upstream()
    {
        var handler = new FakeHttpMessageHandler().Reply("{}", HttpStatusCode.NotFound).Reply("{}", HttpStatusCode.InternalServerError);
        var client = new StravaClient(new HttpClient(handler), new StubTokenProvider());

        await Assert.ThrowsAsync<NotFoundException>(() => client.GetActivityAsync(User, 1));
        await Assert.ThrowsAsync<UpstreamServiceException>(() => client.GetActivityAsync(User, 1));
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public async Task Strava_client_reads_the_time_distance_and_velocity_streams()
    {
        var handler = new FakeHttpMessageHandler().Reply(
            """{"time":{"data":[0,1,2]},"distance":{"data":[0.0,3.1,6.4]},"velocity_smooth":{"data":[3,3.1,3.3]},"latlng":{"data":[[51.5,-0.1],[51.6,-0.2],[51.7,-0.3]]}}""");
        var client = new StravaClient(new HttpClient(handler), new StubTokenProvider());

        var streams = await client.GetStreamsAsync(User, 55);

        Assert.NotNull(streams);
        Assert.Equal([0, 1, 2], streams.TimeSeconds);
        Assert.Equal([0.0, 3.1, 6.4], streams.DistanceMetres);
        Assert.Equal([3, 3.1, 3.3], streams.VelocityMetresPerSecond);
        Assert.Equal([[51.5, -0.1], [51.6, -0.2], [51.7, -0.3]], streams.LatLng.Select(p => p.ToArray()));
        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            "https://www.strava.com/api/v3/activities/55/streams?keys=time,distance,velocity_smooth,latlng&key_by_type=true",
            request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public async Task Strava_client_gives_no_streams_for_an_activity_without_GPS()
    {
        var handler = new FakeHttpMessageHandler()
            .Reply("{}", HttpStatusCode.NotFound)
            .Reply("""{"time":{"data":[0,1,2]}}""")
            .Reply("{}", HttpStatusCode.TooManyRequests);
        var client = new StravaClient(new HttpClient(handler), new StubTokenProvider());

        Assert.Null(await client.GetStreamsAsync(User, 1));
        Assert.Null(await client.GetStreamsAsync(User, 1));
        await Assert.ThrowsAsync<UpstreamServiceException>(() => client.GetStreamsAsync(User, 1));
    }

    [Theory]
    [Trait("Behaviour", "BEH-MAP-3")]
    [InlineData("""{"time":{"data":[0,1]},"distance":{"data":[0,3]}}""")]
    [InlineData("""{"time":{"data":[0,1]},"distance":{"data":[0,3]},"latlng":{"data":[[51.5,-0.1],[51.6]]}}""")]
    [InlineData("""{"time":{"data":[0,1]},"distance":{"data":[0,3]},"latlng":{"data":"nope"}}""")]
    public async Task Strava_client_gives_no_coordinates_when_the_latlng_stream_is_missing_or_malformed(string body)
    {
        var client = new StravaClient(new HttpClient(new FakeHttpMessageHandler().Reply(body)), new StubTokenProvider());

        var streams = await client.GetStreamsAsync(User, 1);

        Assert.NotNull(streams);
        Assert.Empty(streams.LatLng);
        Assert.Equal([0, 1], streams.TimeSeconds);
    }

    [Fact]
    public async Task Spotify_client_reads_plays_asking_for_the_most_recent_fifty_after_the_run_start()
    {
        var handler = new FakeHttpMessageHandler().Reply(PlaysJson);
        var tokens = new StubTokenProvider();
        var client = new SpotifyClient(new HttpClient(handler), tokens);
        var after = new DateTimeOffset(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

        var plays = await client.GetRecentlyPlayedAsync(User, after);

        Assert.Equal([User], tokens.Users);
        Assert.Equal(2, plays.Count);
        Assert.Equal("Song One", plays[0].TrackName);
        Assert.Equal(["A", "B"], plays[0].Artists);
        Assert.Equal("https://img/1.jpg", plays[0].AlbumArtUrl);
        Assert.Equal(240000, plays[0].DurationMs);
        Assert.Equal(new DateTimeOffset(2026, 10, 17, 9, 10, 0, TimeSpan.Zero), plays[0].PlayedAt);
        Assert.Null(plays[1].AlbumArtUrl);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            $"https://api.spotify.com/v1/me/player/recently-played?limit=50&after={after.ToUnixTimeMilliseconds()}",
            request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public async Task Spotify_client_reads_the_context_of_each_play_and_null_for_a_queue()
    {
        var handler = new FakeHttpMessageHandler().Reply("""
            {"items":[
              {"track":{"id":"t1","name":"One","duration_ms":1,"artists":[],"album":{"images":[]}},
               "played_at":"2026-10-17T09:10:00.000Z","context":{"type":"playlist","uri":"spotify:playlist:p1"}},
              {"track":{"id":"t2","name":"Two","duration_ms":1,"artists":[],"album":{"images":[]}},
               "played_at":"2026-10-17T09:05:00.000Z","context":null},
              {"track":{"id":"t3","name":"Three","duration_ms":1,"artists":[],"album":{"images":[]}},
               "played_at":"2026-10-17T09:04:00.000Z"}
            ]}
            """);
        var client = new SpotifyClient(new HttpClient(handler), new StubTokenProvider());

        var plays = await client.GetRecentlyPlayedAsync(User, DateTimeOffset.UtcNow);

        Assert.Equal("playlist", plays[0].ContextType);
        Assert.Equal("spotify:playlist:p1", plays[0].ContextUri);
        Assert.Null(plays[1].ContextType);
        Assert.Null(plays[1].ContextUri);
        Assert.Null(plays[2].ContextUri);
    }

    [Theory]
    [InlineData("album", "spotify:album:a1", "https://api.spotify.com/v1/albums/a1")]
    [InlineData("playlist", "spotify:playlist:p1", "https://api.spotify.com/v1/playlists/p1?fields=name")]
    [InlineData("artist", "spotify:artist:r1", "https://api.spotify.com/v1/artists/r1")]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public async Task Spotify_client_looks_a_context_name_up_with_the_users_token(string type, string uri, string expectedUrl)
    {
        var handler = new FakeHttpMessageHandler().Reply("""{"name":"Abbey Road","id":"x"}""");
        var tokens = new StubTokenProvider();
        var client = new SpotifyClient(new HttpClient(handler), tokens);

        Assert.Equal("Abbey Road", await client.GetContextNameAsync(User, type, uri));

        Assert.Equal([User], tokens.Users);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(expectedUrl, request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public async Task Spotify_client_gives_no_name_when_Spotify_refuses_or_the_type_has_no_lookup()
    {
        var handler = new FakeHttpMessageHandler()
            .Reply("{}", HttpStatusCode.NotFound)
            .Reply("{}", HttpStatusCode.Forbidden)
            .Reply("{}", HttpStatusCode.TooManyRequests);
        var client = new SpotifyClient(new HttpClient(handler), new StubTokenProvider());

        Assert.Null(await client.GetContextNameAsync(User, "playlist", "spotify:playlist:37i9dQZF1DX"));
        Assert.Null(await client.GetContextNameAsync(User, "playlist", "spotify:playlist:private"));
        Assert.Null(await client.GetContextNameAsync(User, "album", "spotify:album:a1"));
        Assert.Null(await client.GetContextNameAsync(User, "show", "spotify:show:s1"));
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Spotify_client_maps_a_failure_to_upstream()
    {
        var handler = new FakeHttpMessageHandler().Reply("{}", HttpStatusCode.Unauthorized);
        var client = new SpotifyClient(new HttpClient(handler), new StubTokenProvider());

        await Assert.ThrowsAsync<UpstreamServiceException>(() => client.GetRecentlyPlayedAsync(User, DateTimeOffset.UtcNow));
    }
}
