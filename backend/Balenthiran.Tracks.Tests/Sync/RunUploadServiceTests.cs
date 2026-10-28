using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunUploadServiceTests
{
    private static readonly DateTimeOffset Start = FitFixture.Start;

    private sealed class FakeSpotify(params SpotifyPlay[] plays) : ISpotifyClient
    {
        public DateTimeOffset? After { get; private set; }

        public Task<string?> GetContextNameAsync(Guid userId, string contextType, string contextUri, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<IReadOnlyList<ISpotifyPlay>> GetRecentlyPlayedAsync(Guid userId, DateTimeOffset after, CancellationToken cancellationToken = default)
        {
            After = after;
            return Task.FromResult<IReadOnlyList<ISpotifyPlay>>(plays);
        }
    }

    private static SpotifyPlay Play(string name, int endsAfterMinutes) => new()
    {
        TrackName = name,
        Artists = ["A"],
        DurationMs = 240000,
        PlayedAt = Start.AddMinutes(endsAfterMinutes),
    };

    private static RunUploadService Build(TestDb db, FakeSpotify spotify) =>
        new(new UserService(db.Context()), new FitFileReader(), spotify, new RunSongMatcher(),
            new RunContextNamer(spotify, NullLogger<RunContextNamer>.Instance), new RunHighlights(), new RunStore(db.Context()),
            NullLogger<RunUploadService>.Instance);

    private static Task<IRun> Upload(RunUploadService service, Guid userId, byte[] file) =>
        service.UploadAsync(userId, new MemoryStream(file));

    private static async Task<IReadOnlyList<IRun>> RunsOf(TestDb db, Guid userId) => await new RunStore(db.Context()).GetAllAsync(userId);

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task An_uploaded_run_is_kept_with_the_songs_that_overlapped_it_its_route_and_its_highlights()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var spotify = new FakeSpotify(Play("late", 20), Play("early", 8), Play("way after", 90));

        await Upload(Build(db, spotify), user.Id, FitFixture.Fit());

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal(Start, run.StartedAt);
        Assert.Equal(1200, run.ElapsedSeconds);
        Assert.Equal("Run", run.SportType);
        Assert.Equal(["early", "late"], run.Songs.Select(s => s.Name));
        Assert.Equal(Start, spotify.After);
        Assert.Equal(FitFixture.Records, run.Route.Count);
        Assert.True(run.StreamsRead);
        // The burst at 1000 s falls in "late" (960 s to 1200 s).
        Assert.Equal([false, true], run.Songs.Select(s => s.IsTopSpeed));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task An_uploaded_run_cannot_collide_with_a_strava_activity_id()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);

        var run = await Upload(Build(db, new FakeSpotify()), user.Id, FitFixture.Fit());

        Assert.True(run.ActivityId < 0);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task A_file_with_no_gps_keeps_its_songs_and_draws_no_map()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);

        await Upload(Build(db, new FakeSpotify(Play("early", 8))), user.Id, FitFixture.Fit(gps: false));

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal(["early"], run.Songs.Select(s => s.Name));
        Assert.Empty(run.Route);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task A_zip_is_read_like_the_fit_inside_it()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);

        await Upload(Build(db, new FakeSpotify(Play("early", 8))), user.Id, FitFixture.Zip(FitFixture.Fit()));

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal(Start, run.StartedAt);
        Assert.Equal(FitFixture.Records, run.Route.Count);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task A_run_kept_from_strava_with_the_same_start_is_replaced_not_duplicated()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        await new RunStore(db.Context()).SaveAsync(user.Id, new Run { ActivityId = 55, Name = "Morning Run", SportType = "Run", StartedAt = Start, ElapsedSeconds = 1190 });

        await Upload(Build(db, new FakeSpotify(Play("early", 8))), user.Id, FitFixture.Fit());

        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal("Garmin run", run.Name);
        Assert.NotEqual(55, run.ActivityId);
        Assert.Equal(["early"], run.Songs.Select(s => s.Name));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task Uploading_the_same_run_again_replaces_it_and_leaves_other_runs_alone()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var other = await db.AddUserAsync(2);
        await new RunStore(db.Context()).SaveAsync(other.Id, new Run { ActivityId = 7, Name = "theirs", StartedAt = Start });
        await new RunStore(db.Context()).SaveAsync(user.Id, new Run { ActivityId = 8, Name = "earlier", StartedAt = Start.AddDays(-1) });
        var service = Build(db, new FakeSpotify(Play("early", 8)));

        await Upload(service, user.Id, FitFixture.Fit());
        await Upload(service, user.Id, FitFixture.Fit());

        Assert.Equal(["Garmin run", "earlier"], (await RunsOf(db, user.Id)).Select(r => r.Name));
        Assert.Equal("theirs", Assert.Single(await RunsOf(db, other.Id)).Name);
        Assert.Single((await RunsOf(db, user.Id))[0].Songs);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task A_file_that_is_not_a_run_is_refused_and_nothing_is_kept()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);

        await Assert.ThrowsAsync<ValidationException>(() => Upload(Build(db, new FakeSpotify()), user.Id, "not a fit file"u8.ToArray()));

        Assert.Empty(await RunsOf(db, user.Id));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task Without_spotify_connected_the_upload_is_refused_because_songs_cannot_be_matched()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1, spotifyRefresh: null);

        await Assert.ThrowsAsync<ValidationException>(() => Upload(Build(db, new FakeSpotify()), user.Id, FitFixture.Fit()));

        Assert.Empty(await RunsOf(db, user.Id));
    }
}
