using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunSyncProcessorTests
{
    private const long Athlete = 4242;

    private static readonly DateTimeOffset Start = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    private sealed class FakeStrava(StravaActivity activity, ActivityStreams? streams = null, bool streamsFail = false) : IStravaClient
    {
        public List<Guid> Users { get; } = [];

        public Task<IStravaActivity> GetActivityAsync(Guid userId, long activityId, CancellationToken cancellationToken = default)
        {
            Users.Add(userId);
            return Task.FromResult<IStravaActivity>(activity);
        }

        public Task<IActivityStreams?> GetStreamsAsync(Guid userId, long activityId, CancellationToken cancellationToken = default) =>
            streamsFail
                ? throw new UpstreamServiceException("Strava streams request failed with status 429.")
                : Task.FromResult<IActivityStreams?>(streams);
    }

    private sealed class FakeSpotify(params SpotifyPlay[] plays) : ISpotifyClient
    {
        public List<Guid> Users { get; } = [];

        /// <summary>What the name lookup answers; null is a 404, <see cref="NameLookupFails"/> an unreachable Spotify.</summary>
        public string? ContextName { get; init; }

        public bool NameLookupFails { get; init; }

        public List<(string Type, string Uri)> NameLookups { get; } = [];

        public Task<string?> GetContextNameAsync(Guid userId, string contextType, string contextUri, CancellationToken cancellationToken = default)
        {
            NameLookups.Add((contextType, contextUri));
            return NameLookupFails
                ? throw new UpstreamServiceException("Spotify was unreachable.")
                : Task.FromResult(ContextName);
        }

        public DateTimeOffset? After { get; private set; }

        public Task<IReadOnlyList<ISpotifyPlay>> GetRecentlyPlayedAsync(Guid userId, DateTimeOffset after, CancellationToken cancellationToken = default)
        {
            Users.Add(userId);
            After = after;
            return Task.FromResult<IReadOnlyList<ISpotifyPlay>>(plays);
        }
    }

    private static StravaActivity Activity(string sport = "Run") => new()
    {
        Id = 55, Name = "Morning Run", SportType = sport, StartDate = Start, ElapsedTimeSeconds = 1800,
    };

    private static SpotifyPlay Play(string name, int endsAfterMinutes) => new()
    {
        TrackName = name, Artists = ["A"], DurationMs = 240000, PlayedAt = Start.AddMinutes(endsAfterMinutes),
    };

    private static SpotifyPlay PlayFrom(string name, int endsAfterMinutes, string? type, string? uri)
    {
        var play = Play(name, endsAfterMinutes);
        play.ContextType = type;
        play.ContextUri = uri;
        return play;
    }

    private static StravaSettings ConfiguredStrava => new() { ClientId = "i", ClientSecret = "s" };

    private static SpotifySettings ConfiguredSpotify => new() { ClientId = "i", ClientSecret = "s" };

    private static RunSyncProcessor Build(
        TestDb db, FakeStrava strava, FakeSpotify spotify, StravaSettings? stravaSettings = null, SpotifySettings? spotifySettings = null) =>
        new(stravaSettings ?? ConfiguredStrava, spotifySettings ?? ConfiguredSpotify, new UserService(db.Context()), strava, spotify,
            new RunSongMatcher(), new RunContextNamer(spotify, NullLogger<RunContextNamer>.Instance), new RunHighlights(), new RunStore(db.Context()), NullLogger<RunSyncProcessor>.Instance);

    private static async Task<IReadOnlyList<IRun>> RunsOf(TestDb db, Guid userId) => await new RunStore(db.Context()).GetAllAsync(userId);

    [Fact]
    public async Task A_run_is_stored_for_the_athletes_user_with_the_songs_that_overlapped_it()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);
        var other = await db.AddUserAsync(1);
        var strava = new FakeStrava(Activity());
        var spotify = new FakeSpotify(Play("late", 20), Play("early", 8), Play("way after", 90));

        await Build(db, strava, spotify).ProcessAsync(Athlete, 55);

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal("Morning Run", run.Name);
        Assert.Equal(1800, run.ElapsedSeconds);
        Assert.Equal(["early", "late"], run.Songs.Select(s => s.Name));
        Assert.Equal(Start, spotify.After);
        Assert.Equal([user.Id], strava.Users);
        Assert.Equal([user.Id], spotify.Users);
        Assert.Empty(await RunsOf(db, other.Id));
    }

    [Fact]
    public async Task A_run_with_no_songs_is_still_stored()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);

        await Build(db, new FakeStrava(Activity()), new FakeSpotify()).ProcessAsync(Athlete, 55);

        Assert.Empty(Assert.Single(await RunsOf(db, user.Id)).Songs);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public async Task A_run_whose_songs_mostly_share_a_context_is_stored_with_its_type_and_name()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);
        var spotify = new FakeSpotify(
            PlayFrom("a", 8, "album", "spotify:album:abc"), PlayFrom("b", 12, "album", "spotify:album:abc"), PlayFrom("c", 20, null, null))
        { ContextName = "Abbey Road" };

        await Build(db, new FakeStrava(Activity()), spotify).ProcessAsync(Athlete, 55);

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal("album", run.ContextType);
        Assert.Equal("Abbey Road", run.ContextName);
        Assert.Equal([("album", "spotify:album:abc")], spotify.NameLookups);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public async Task A_run_whose_songs_are_mixed_is_stored_as_mixed_without_a_lookup()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);
        var spotify = new FakeSpotify(PlayFrom("a", 8, "album", "spotify:album:abc"), PlayFrom("b", 12, "playlist", "spotify:playlist:xyz"));

        await Build(db, new FakeStrava(Activity()), spotify).ProcessAsync(Athlete, 55);

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal("mixed", run.ContextType);
        Assert.Null(run.ContextName);
        Assert.Empty(spotify.NameLookups);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public async Task A_context_name_that_cannot_be_read_still_stores_the_run_with_its_type(bool unreachable)
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);
        // Null name = Spotify answered 403/404 (a private or editorial playlist); unreachable throws.
        var spotify = new FakeSpotify(PlayFrom("a", 8, "playlist", "spotify:playlist:xyz")) { ContextName = null, NameLookupFails = unreachable };

        await Build(db, new FakeStrava(Activity()), spotify).ProcessAsync(Athlete, 55);

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal("playlist", run.ContextType);
        Assert.Null(run.ContextName);
        Assert.Equal(["a"], run.Songs.Select(s => s.Name));
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public async Task A_run_with_no_song_context_stores_none()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);

        await Build(db, new FakeStrava(Activity()), new FakeSpotify(Play("queued", 8))).ProcessAsync(Athlete, 55);

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Null(run.ContextType);
        Assert.Null(run.ContextName);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    [Trait("Behaviour", "BEH-HIGH-4")]
    public async Task Songs_stored_carry_the_highlights_picked_from_the_streams()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);
        // "early" plays 4:00-8:00 into the run (running at 3 m/s); "late" plays 16:00-20:00 (at 4 m/s, so
        // it has both the top speed and the fastest pace).
        double[] time = [.. Enumerable.Range(0, 1801).Select(s => (double)s)];
        double[] velocity = [.. time.Select(s => s is >= 960 and <= 1200 ? 4.0 : 3.0)];
        var distance = new double[time.Length];
        for (var i = 1; i < time.Length; i++)
            distance[i] = distance[i - 1] + velocity[i];
        var streams = new ActivityStreams { TimeSeconds = time, DistanceMetres = distance, VelocityMetresPerSecond = velocity };

        await Build(db, new FakeStrava(Activity(), streams), new FakeSpotify(Play("late", 20), Play("early", 8))).ProcessAsync(Athlete, 55);

        var songs = Assert.Single(await RunsOf(db, user.Id)).Songs;
        Assert.Equal(["early", "late"], songs.Select(s => s.Name));
        Assert.Equal([false, true], songs.Select(s => s.IsTopSpeed));
        Assert.Equal([false, true], songs.Select(s => s.IsFastestPace));
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public async Task A_run_whose_streams_cannot_be_read_is_still_stored_without_highlights()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);

        await Build(db, new FakeStrava(Activity(), streamsFail: true), new FakeSpotify(Play("early", 8))).ProcessAsync(Athlete, 55);

        var song = Assert.Single(Assert.Single(await RunsOf(db, user.Id)).Songs);
        Assert.Equal("early", song.Name);
        Assert.False(song.IsTopSpeed);
        Assert.False(song.IsFastestPace);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public async Task A_run_with_no_GPS_names_no_song()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);

        await Build(db, new FakeStrava(Activity(), streams: null), new FakeSpotify(Play("early", 8))).ProcessAsync(Athlete, 55);

        var song = Assert.Single(Assert.Single(await RunsOf(db, user.Id)).Songs);
        Assert.False(song.IsTopSpeed);
        Assert.False(song.IsFastestPace);
    }

    private static ActivityStreams StreamsWithRoute(int seconds) => new()
    {
        TimeSeconds = [.. Enumerable.Range(0, seconds + 1).Select(s => (double)s)],
        DistanceMetres = [.. Enumerable.Range(0, seconds + 1).Select(s => s * 3.0)],
        VelocityMetresPerSecond = [.. Enumerable.Range(0, seconds + 1).Select(_ => 3.0)],
        LatLng = [.. Enumerable.Range(0, seconds + 1).Select(s => (IReadOnlyList<double>)[51.5 + (s * 0.00001), -0.1])],
    };

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    [Trait("Behaviour", "BEH-SYNC-6")]
    public async Task A_run_is_stored_with_its_route_and_marked_as_read()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);

        await Build(db, new FakeStrava(Activity(), StreamsWithRoute(1800)), new FakeSpotify(Play("early", 8))).ProcessAsync(Athlete, 55);

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal(RunRoute.MaxPoints, run.Route.Count);
        Assert.Equal(0, run.Route[0].Seconds);
        Assert.Equal(1800, run.Route[^1].Seconds);
        Assert.True(run.StreamsRead);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public async Task A_run_with_no_songs_still_keeps_its_route()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);

        await Build(db, new FakeStrava(Activity(), StreamsWithRoute(600)), new FakeSpotify()).ProcessAsync(Athlete, 55);

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Empty(run.Songs);
        Assert.NotEmpty(run.Route);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-6")]
    public async Task A_run_whose_streams_cannot_be_read_is_left_for_the_backfill()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);

        await Build(db, new FakeStrava(Activity(), streamsFail: true), new FakeSpotify(Play("early", 8))).ProcessAsync(Athlete, 55);

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Empty(run.Route);
        Assert.False(run.StreamsRead);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-3")]
    public async Task A_run_with_no_GPS_is_read_once_and_has_no_route()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);

        await Build(db, new FakeStrava(Activity(), streams: null), new FakeSpotify(Play("early", 8))).ProcessAsync(Athlete, 55);

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Empty(run.Route);
        Assert.True(run.StreamsRead);
    }

    [Theory]
    [InlineData("Ride")]
    [InlineData("Swim")]
    public async Task A_non_run_is_ignored_without_asking_Spotify(string sport)
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);
        var spotify = new FakeSpotify();

        await Build(db, new FakeStrava(Activity(sport)), spotify).ProcessAsync(Athlete, 55);

        Assert.Empty(await RunsOf(db, user.Id));
        Assert.Empty(spotify.Users);
    }

    [Theory]
    [InlineData("Run")]
    [InlineData("TrailRun")]
    [InlineData("VirtualRun")]
    [InlineData("trailrun")]
    public async Task The_default_run_sport_types_count(string sport)
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);

        await Build(db, new FakeStrava(Activity(sport)), new FakeSpotify()).ProcessAsync(Athlete, 55);

        Assert.Single(await RunsOf(db, user.Id));
    }

    [Fact]
    public async Task Configured_sport_types_replace_the_defaults()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);
        var settings = ConfiguredStrava;
        settings.SportTypes = ["Walk"];

        await Build(db, new FakeStrava(Activity("Run")), new FakeSpotify(), settings).ProcessAsync(Athlete, 55);
        await Build(db, new FakeStrava(Activity("Walk")), new FakeSpotify(), settings).ProcessAsync(Athlete, 55);

        Assert.Equal("Walk", Assert.Single(await RunsOf(db, user.Id)).SportType);
    }

    [Fact]
    public async Task Missing_app_credentials_skip_processing_without_calling_either_API()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete);
        var strava = new FakeStrava(Activity());
        var spotify = new FakeSpotify();

        await Build(db, strava, spotify, new StravaSettings()).ProcessAsync(Athlete, 55);
        await Build(db, strava, spotify, spotifySettings: new SpotifySettings()).ProcessAsync(Athlete, 55);

        Assert.Empty(strava.Users);
        Assert.Empty(spotify.Users);
        Assert.Empty(await RunsOf(db, user.Id));
    }

    [Fact]
    public async Task An_athlete_who_is_not_a_user_is_skipped_without_calling_either_API()
    {
        using var db = new TestDb();
        var strava = new FakeStrava(Activity());
        var spotify = new FakeSpotify();

        await Build(db, strava, spotify).ProcessAsync(Athlete, 55);

        Assert.Empty(strava.Users);
        Assert.Empty(spotify.Users);
    }

    [Fact]
    public async Task A_user_without_Spotify_connected_is_skipped_without_calling_either_API()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(Athlete, spotifyRefresh: null);
        var strava = new FakeStrava(Activity());
        var spotify = new FakeSpotify();

        await Build(db, strava, spotify).ProcessAsync(Athlete, 55);

        Assert.Empty(strava.Users);
        Assert.Empty(spotify.Users);
        Assert.Empty(await RunsOf(db, user.Id));
    }
}
