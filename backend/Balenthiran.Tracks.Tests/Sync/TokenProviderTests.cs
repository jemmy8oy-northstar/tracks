using System.Net;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

public class TokenProviderTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);

    private static string StravaReply(string access, DateTimeOffset expiresAt, string refresh = "strava-1") =>
        $$"""{"access_token":"{{access}}","refresh_token":"{{refresh}}","expires_at":{{expiresAt.ToUnixTimeSeconds()}}}""";

    private static string SpotifyReply(string access, int expiresIn = 3600, string? refresh = null) =>
        refresh is null
            ? $$"""{"access_token":"{{access}}","expires_in":{{expiresIn}}}"""
            : $$"""{"access_token":"{{access}}","expires_in":{{expiresIn}},"refresh_token":"{{refresh}}"}""";

    private static StravaTokenProvider Strava(TestDb db, FakeHttpMessageHandler handler, ManualTimeProvider time) =>
        new(new HttpClient(handler), db.Scopes, new StravaSettings { ClientId = "id", ClientSecret = "shh" }, time);

    private static SpotifyTokenProvider Spotify(TestDb db, FakeHttpMessageHandler handler, ManualTimeProvider time) =>
        new(new HttpClient(handler), db.Scopes, new SpotifySettings { ClientId = "id", ClientSecret = "shh" }, time);

    [Fact]
    public async Task Strava_exchanges_the_users_refresh_token_once_and_caches_the_result()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var time = new ManualTimeProvider(Start);
        var handler = new FakeHttpMessageHandler().Reply(StravaReply("access-1", Start.AddHours(6), refresh: "strava-0"));
        var provider = Strava(db, handler, time);

        Assert.Equal("access-1", await provider.GetAccessTokenAsync(user.Id));
        time.Advance(TimeSpan.FromHours(5));
        Assert.Equal("access-1", await provider.GetAccessTokenAsync(user.Id));

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://www.strava.com/oauth/token", request.RequestUri!.ToString());
        Assert.Contains("grant_type=refresh_token", handler.RequestBodies[0]);
        Assert.Contains("refresh_token=strava-0", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task Each_user_is_exchanged_with_and_cached_under_their_own_token()
    {
        using var db = new TestDb();
        var ann = await db.AddUserAsync(1, stravaRefresh: "ann-refresh");
        var bob = await db.AddUserAsync(2, stravaRefresh: "bob-refresh");
        var handler = new FakeHttpMessageHandler()
            .Reply(StravaReply("ann-access", Start.AddHours(6), refresh: "ann-refresh"))
            .Reply(StravaReply("bob-access", Start.AddHours(6), refresh: "bob-refresh"));
        var provider = Strava(db, handler, new ManualTimeProvider(Start));

        Assert.Equal("ann-access", await provider.GetAccessTokenAsync(ann.Id));
        Assert.Equal("bob-access", await provider.GetAccessTokenAsync(bob.Id));
        Assert.Equal("ann-access", await provider.GetAccessTokenAsync(ann.Id));

        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("refresh_token=ann-refresh", handler.RequestBodies[0]);
        Assert.Contains("refresh_token=bob-refresh", handler.RequestBodies[1]);
    }

    [Fact]
    public async Task A_rotated_Strava_refresh_token_is_persisted_and_used_for_the_next_exchange()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var time = new ManualTimeProvider(Start);
        var handler = new FakeHttpMessageHandler()
            .Reply(StravaReply("access-1", Start.AddHours(6), refresh: "strava-1"))
            .Reply(StravaReply("access-2", Start.AddHours(12), refresh: "strava-2"));
        var provider = Strava(db, handler, time);

        await provider.GetAccessTokenAsync(user.Id);
        Assert.Equal("strava-1", (await db.ReloadAsync(user.Id)).StravaRefreshToken);
        time.Advance(TimeSpan.FromHours(6).Subtract(TimeSpan.FromSeconds(30)));

        // Inside the 60 second safety margin, so the cached token is no longer trusted.
        Assert.Equal("access-2", await provider.GetAccessTokenAsync(user.Id));
        Assert.Contains("refresh_token=strava-1", handler.RequestBodies[1]);
        Assert.Equal("strava-2", (await db.ReloadAsync(user.Id)).StravaRefreshToken);
    }

    [Fact]
    public async Task Spotify_caches_for_the_lifetime_it_reports_then_refreshes()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var time = new ManualTimeProvider(Start);
        var handler = new FakeHttpMessageHandler()
            .Reply(SpotifyReply("access-1", expiresIn: 3600))
            .Reply(SpotifyReply("access-2", refresh: "spotify-new"));
        var provider = Spotify(db, handler, time);

        Assert.Equal("access-1", await provider.GetAccessTokenAsync(user.Id));
        time.Advance(TimeSpan.FromMinutes(30));
        Assert.Equal("access-1", await provider.GetAccessTokenAsync(user.Id));
        Assert.Single(handler.Requests);

        time.Advance(TimeSpan.FromMinutes(30));
        Assert.Equal("access-2", await provider.GetAccessTokenAsync(user.Id));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Spotify_authenticates_with_basic_credentials_and_keeps_the_stored_token_when_none_is_returned()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var time = new ManualTimeProvider(Start);
        var handler = new FakeHttpMessageHandler()
            .Reply(SpotifyReply("access-1", expiresIn: 60))
            .Reply(SpotifyReply("access-2"));
        var provider = Spotify(db, handler, time);

        await provider.GetAccessTokenAsync(user.Id);
        time.Advance(TimeSpan.FromMinutes(5));
        await provider.GetAccessTokenAsync(user.Id);

        Assert.Equal("Basic", handler.Requests[0].Headers.Authorization!.Scheme);
        Assert.Equal("https://accounts.spotify.com/api/token", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains("refresh_token=spotify-0", handler.RequestBodies[1]);
        Assert.Equal("spotify-0", (await db.ReloadAsync(user.Id)).SpotifyRefreshToken);
    }

    [Fact]
    public async Task A_rotated_Spotify_refresh_token_is_persisted()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var handler = new FakeHttpMessageHandler().Reply(SpotifyReply("access-1", refresh: "spotify-new"));

        await Spotify(db, handler, new ManualTimeProvider(Start)).GetAccessTokenAsync(user.Id);

        Assert.Equal("spotify-new", (await db.ReloadAsync(user.Id)).SpotifyRefreshToken);
    }

    [Fact]
    public async Task A_user_without_Spotify_connected_is_not_found_and_nothing_is_sent()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1, spotifyRefresh: null);
        var handler = new FakeHttpMessageHandler();

        await Assert.ThrowsAsync<NotFoundException>(() => Spotify(db, handler, new ManualTimeProvider(Start)).GetAccessTokenAsync(user.Id));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task An_unknown_user_is_not_found()
    {
        using var db = new TestDb();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Strava(db, new FakeHttpMessageHandler(), new ManualTimeProvider(Start)).GetAccessTokenAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task A_failed_exchange_is_an_upstream_failure_that_does_not_leak_the_credentials()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var handler = new FakeHttpMessageHandler().Reply("""{"message":"Bad Request"}""", HttpStatusCode.BadRequest);
        var provider = Strava(db, handler, new ManualTimeProvider(Start));

        var error = await Assert.ThrowsAsync<UpstreamServiceException>(() => provider.GetAccessTokenAsync(user.Id));

        Assert.DoesNotContain("shh", error.Message);
        Assert.DoesNotContain("strava-0", error.Message);
    }
}
