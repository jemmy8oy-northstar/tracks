using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunStreamsBackfillTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 20, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Streams by activity id; an id that is absent is a 404 (null), one in <see cref="Failing"/> throws.</summary>
    private sealed class FakeStrava : IStravaClient
    {
        public Dictionary<long, ActivityStreams> Streams { get; } = [];
        public HashSet<long> Failing { get; } = [];
        public List<long> Asked { get; } = [];

        public Task<IStravaActivity> GetActivityAsync(Guid userId, long activityId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("the backfill never re-reads the activity");

        public Task UpdateDescriptionAsync(Guid userId, long activityId, string description, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("the backfill never writes to Strava");

        public Task<IActivityStreams?> GetStreamsAsync(Guid userId, long activityId, CancellationToken cancellationToken = default)
        {
            Asked.Add(activityId);
            if (Failing.Contains(activityId))
                throw new UpstreamServiceException("Strava streams request failed with status 429.");
            return Task.FromResult<IActivityStreams?>(Streams.GetValueOrDefault(activityId));
        }
    }

    // "early" plays 4:00-8:00 at 3 m/s, "late" 16:00-20:00 at 4 m/s, so "late" holds both highlights.
    private static ActivityStreams FastLate()
    {
        double[] time = [.. Enumerable.Range(0, 1801).Select(s => (double)s)];
        double[] velocity = [.. time.Select(s => s is >= 960 and <= 1200 ? 4.0 : 3.0)];
        var distance = new double[time.Length];
        for (var i = 1; i < time.Length; i++)
            distance[i] = distance[i - 1] + velocity[i];
        return new ActivityStreams
        {
            TimeSeconds = time, DistanceMetres = distance, VelocityMetresPerSecond = velocity,
            LatLng = [.. time.Select(s => (IReadOnlyList<double>)[51.5 + (s * 0.00001), -0.1])],
        };
    }

    private static RunSong Song(string name, int offset) => new()
    {
        Name = name, Artists = ["A"], PlayedFrom = Start.AddSeconds(offset), PlayedTo = Start.AddSeconds(offset + 240), OffsetSeconds = offset,
    };

    // A run kept before BEH-SYNC-6: no route, streams never read.
    private static Run OldRun(long id, int daysAgo = 0) => new()
    {
        ActivityId = id, Name = $"run {id}", SportType = "Run", StartedAt = Start.AddDays(-daysAgo), ElapsedSeconds = 1800,
        Songs = [Song("early", 240), Song("late", 960)],
    };

    private static RunStreamsBackfill Build(TestDb db, FakeStrava strava, bool configured = true) => new(
        configured ? new StravaSettings { ClientId = "i", ClientSecret = "s" } : new StravaSettings(),
        strava, new RunHighlights(), new RunStore(db.Context()), NullLogger<RunStreamsBackfill>.Instance);

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-6")]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public async Task A_run_kept_before_highlights_existed_is_given_them_and_its_route()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        await new RunStore(db.Context()).SaveAsync(user.Id, OldRun(7));
        var strava = new FakeStrava { Streams = { [7] = FastLate() } };

        var filled = await Build(db, strava).RunOnceAsync(5, TimeSpan.Zero, new HashSet<long>());

        Assert.Equal(1, filled);
        var run = Assert.Single(await new RunStore(db.Context()).GetAllAsync(user.Id));
        Assert.True(run.StreamsRead);
        Assert.NotEmpty(run.Route);
        Assert.Equal([false, true], run.Songs.Select(s => s.IsTopSpeed));
        Assert.Equal([false, true], run.Songs.Select(s => s.IsFastestPace));
        Assert.Equal(["early", "late"], run.Songs.Select(s => s.Name));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-6")]
    public async Task A_run_already_read_is_never_asked_for_again()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var read = OldRun(8);
        read.StreamsRead = true;
        await new RunStore(db.Context()).SaveAsync(user.Id, read);
        var strava = new FakeStrava();

        Assert.Equal(0, await Build(db, strava).RunOnceAsync(5, TimeSpan.Zero, new HashSet<long>()));
        Assert.Empty(strava.Asked);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-6")]
    [Trait("Behaviour", "BEH-MAP-3")]
    public async Task A_run_Strava_has_no_streams_for_is_tried_once_and_left_as_it_is()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        await new RunStore(db.Context()).SaveAsync(user.Id, OldRun(9));
        var strava = new FakeStrava();

        Assert.Equal(1, await Build(db, strava).RunOnceAsync(5, TimeSpan.Zero, new HashSet<long>()));
        Assert.Equal(0, await Build(db, strava).RunOnceAsync(5, TimeSpan.Zero, new HashSet<long>()));

        Assert.Equal([9L], strava.Asked);
        var run = Assert.Single(await new RunStore(db.Context()).GetAllAsync(user.Id));
        Assert.True(run.StreamsRead);
        Assert.Empty(run.Route);
        Assert.All(run.Songs, s => Assert.False(s.IsTopSpeed));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-6")]
    public async Task A_run_whose_streams_fail_is_skipped_for_the_process_and_stays_unread()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        await new RunStore(db.Context()).SaveAsync(user.Id, OldRun(10, daysAgo: 0));
        await new RunStore(db.Context()).SaveAsync(user.Id, OldRun(11, daysAgo: 1));
        var strava = new FakeStrava { Failing = { 10 }, Streams = { [11] = FastLate() } };
        var skip = new HashSet<long>();

        Assert.Equal(1, await Build(db, strava).RunOnceAsync(5, TimeSpan.Zero, skip));
        Assert.Equal(0, await Build(db, strava).RunOnceAsync(5, TimeSpan.Zero, skip));

        Assert.Equal([10L, 11L], strava.Asked);
        Assert.Equal([10L], skip);
        var runs = await new RunStore(db.Context()).GetAllAsync(user.Id);
        Assert.False(runs.Single(r => r.ActivityId == 10).StreamsRead);
        Assert.True(runs.Single(r => r.ActivityId == 11).StreamsRead);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-6")]
    public async Task A_pass_reads_at_most_its_limit_newest_first_across_users()
    {
        using var db = new TestDb();
        var ann = await db.AddUserAsync(1);
        var bob = await db.AddUserAsync(2);
        await new RunStore(db.Context()).SaveAsync(ann.Id, OldRun(1, daysAgo: 3));
        await new RunStore(db.Context()).SaveAsync(bob.Id, OldRun(2, daysAgo: 1));
        await new RunStore(db.Context()).SaveAsync(ann.Id, OldRun(3, daysAgo: 2));
        var strava = new FakeStrava();

        Assert.Equal(2, await Build(db, strava).RunOnceAsync(2, TimeSpan.Zero, new HashSet<long>()));

        Assert.Equal([2L, 3L], strava.Asked);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-6")]
    public async Task Without_Strava_credentials_nothing_is_read()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        await new RunStore(db.Context()).SaveAsync(user.Id, OldRun(4));
        var strava = new FakeStrava();

        Assert.Equal(0, await Build(db, strava, configured: false).RunOnceAsync(5, TimeSpan.Zero, new HashSet<long>()));
        Assert.Empty(strava.Asked);
    }
}
