using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunSyncProcessorTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    private sealed class FakeStrava(StravaActivity activity) : IStravaClient
    {
        public int Calls { get; private set; }

        public Task<IStravaActivity> GetActivityAsync(long activityId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<IStravaActivity>(activity);
        }
    }

    private sealed class FakeSpotify(params SpotifyPlay[] plays) : ISpotifyClient
    {
        public int Calls { get; private set; }

        public DateTimeOffset? After { get; private set; }

        public Task<IReadOnlyList<ISpotifyPlay>> GetRecentlyPlayedAsync(DateTimeOffset after, CancellationToken cancellationToken = default)
        {
            Calls++;
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

    private static StravaSettings ConfiguredStrava => new() { ClientId = "i", ClientSecret = "s", RefreshToken = "r" };

    private static SpotifySettings ConfiguredSpotify => new() { ClientId = "i", ClientSecret = "s", RefreshToken = "r" };

    private static RunSyncProcessor Build(
        FakeStrava strava, FakeSpotify spotify, RunStore store, StravaSettings? stravaSettings = null, SpotifySettings? spotifySettings = null) =>
        new(stravaSettings ?? ConfiguredStrava, spotifySettings ?? ConfiguredSpotify, strava, spotify, new RunSongMatcher(), store,
            NullLogger<RunSyncProcessor>.Instance);

    [Fact]
    public async Task A_run_is_stored_with_the_songs_that_overlapped_it_and_Spotify_is_asked_from_its_start()
    {
        var strava = new FakeStrava(Activity());
        var spotify = new FakeSpotify(Play("late", 20), Play("early", 8), Play("way after", 90));
        var store = new RunStore();

        await Build(strava, spotify, store).ProcessAsync(55);

        var run = Assert.Single(store.GetAll());
        Assert.Equal("Morning Run", run.Name);
        Assert.Equal(1800, run.ElapsedSeconds);
        Assert.Equal(["early", "late"], run.Songs.Select(s => s.Name));
        Assert.Equal(Start, spotify.After);
    }

    [Fact]
    public async Task A_run_with_no_songs_is_still_stored()
    {
        var store = new RunStore();

        await Build(new FakeStrava(Activity()), new FakeSpotify(), store).ProcessAsync(55);

        Assert.Empty(Assert.Single(store.GetAll()).Songs);
    }

    [Theory]
    [InlineData("Ride")]
    [InlineData("Swim")]
    public async Task A_non_run_is_ignored_without_asking_Spotify(string sport)
    {
        var spotify = new FakeSpotify();
        var store = new RunStore();

        await Build(new FakeStrava(Activity(sport)), spotify, store).ProcessAsync(55);

        Assert.Empty(store.GetAll());
        Assert.Equal(0, spotify.Calls);
    }

    [Theory]
    [InlineData("Run")]
    [InlineData("TrailRun")]
    [InlineData("VirtualRun")]
    [InlineData("trailrun")]
    public async Task The_default_run_sport_types_count(string sport)
    {
        var store = new RunStore();

        await Build(new FakeStrava(Activity(sport)), new FakeSpotify(), store).ProcessAsync(55);

        Assert.Single(store.GetAll());
    }

    [Fact]
    public async Task Configured_sport_types_replace_the_defaults()
    {
        var store = new RunStore();
        var settings = ConfiguredStrava;
        settings.SportTypes = ["Walk"];

        await Build(new FakeStrava(Activity("Run")), new FakeSpotify(), store, settings).ProcessAsync(55);
        await Build(new FakeStrava(Activity("Walk")), new FakeSpotify(), store, settings).ProcessAsync(55);

        Assert.Equal("Walk", Assert.Single(store.GetAll()).SportType);
    }

    [Fact]
    public async Task Missing_credentials_skip_processing_without_calling_either_API()
    {
        var strava = new FakeStrava(Activity());
        var spotify = new FakeSpotify();
        var store = new RunStore();

        await Build(strava, spotify, store, new StravaSettings()).ProcessAsync(55);
        await Build(strava, spotify, store, spotifySettings: new SpotifySettings()).ProcessAsync(55);

        Assert.Equal(0, strava.Calls);
        Assert.Equal(0, spotify.Calls);
        Assert.Empty(store.GetAll());
    }
}
