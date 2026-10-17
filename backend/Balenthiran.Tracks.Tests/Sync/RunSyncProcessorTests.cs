using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunSyncProcessorTests
{
    private const long Athlete = 4242;

    private static readonly DateTimeOffset Start = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    private sealed class FakeStrava(StravaActivity activity) : IStravaClient
    {
        public List<Guid> Users { get; } = [];

        public Task<IStravaActivity> GetActivityAsync(Guid userId, long activityId, CancellationToken cancellationToken = default)
        {
            Users.Add(userId);
            return Task.FromResult<IStravaActivity>(activity);
        }
    }

    private sealed class FakeSpotify(params SpotifyPlay[] plays) : ISpotifyClient
    {
        public List<Guid> Users { get; } = [];

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

    private static StravaSettings ConfiguredStrava => new() { ClientId = "i", ClientSecret = "s" };

    private static SpotifySettings ConfiguredSpotify => new() { ClientId = "i", ClientSecret = "s" };

    private static RunSyncProcessor Build(
        TestDb db, FakeStrava strava, FakeSpotify spotify, StravaSettings? stravaSettings = null, SpotifySettings? spotifySettings = null) =>
        new(stravaSettings ?? ConfiguredStrava, spotifySettings ?? ConfiguredSpotify, new UserService(db.Context()), strava, spotify,
            new RunSongMatcher(), new RunStore(db.Context()), NullLogger<RunSyncProcessor>.Instance);

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
