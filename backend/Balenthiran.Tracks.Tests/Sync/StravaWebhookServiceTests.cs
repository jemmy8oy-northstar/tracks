using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class StravaWebhookServiceTests
{
    private const long Athlete = 4242;

    private static (StravaWebhookService Service, RunSyncQueue Queue) Build(TestDb db, string? verifyToken = "secret")
    {
        var queue = new RunSyncQueue();
        var settings = new StravaSettings { VerifyToken = verifyToken };
        return (new StravaWebhookService(settings, queue, new UserService(db.Context()), NullLogger<StravaWebhookService>.Instance), queue);
    }

    private static StravaWebhookEvent Event(string objectType = "activity", string aspect = "create", long owner = Athlete, long id = 99) =>
        new() { ObjectType = objectType, AspectType = aspect, OwnerId = owner, ObjectId = id };

    private static StravaWebhookEvent Deauthorisation(string authorized = "false", long owner = Athlete) =>
        new() { ObjectType = "athlete", AspectType = "update", OwnerId = owner, ObjectId = owner, Updates = new() { ["authorized"] = authorized } };

    private static List<(long AthleteId, long ActivityId)> Queued(RunSyncQueue queue)
    {
        // The reader blocks once the queue is empty, so a short timeout marks the end of what was queued.
        var queued = new List<(long AthleteId, long ActivityId)>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        try
        {
            foreach (var item in queue.ReadAllAsync(cts.Token).ToBlockingEnumerable(cts.Token))
                queued.Add(item);
        }
        catch (OperationCanceledException)
        {
        }

        return queued;
    }

    [Fact]
    public void Subscribe_with_the_configured_token_is_valid()
    {
        using var db = new TestDb();

        Assert.True(Build(db).Service.IsValidSubscription("subscribe", "secret"));
    }

    [Theory]
    [InlineData("subscribe", "wrong")]
    [InlineData("subscribe", null)]
    [InlineData("unsubscribe", "secret")]
    [InlineData(null, "secret")]
    public void Anything_else_is_refused(string? mode, string? token)
    {
        using var db = new TestDb();

        Assert.False(Build(db).Service.IsValidSubscription(mode, token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void With_no_token_configured_nothing_is_ever_valid(string? configured)
    {
        using var db = new TestDb();

        Assert.False(Build(db, verifyToken: configured).Service.IsValidSubscription("subscribe", configured));
    }

    [Fact]
    public async Task A_new_activity_from_a_known_athlete_is_queued_with_that_athlete()
    {
        using var db = new TestDb();
        await db.AddUserAsync(Athlete);
        var (service, queue) = Build(db);

        await service.AcceptAsync(Event());

        Assert.Equal([(Athlete, 99L)], Queued(queue));
    }

    [Fact]
    public async Task A_new_activity_from_an_athlete_who_never_signed_in_is_ignored()
    {
        using var db = new TestDb();
        await db.AddUserAsync(Athlete);
        var (service, queue) = Build(db);

        await service.AcceptAsync(Event(owner: 1));

        Assert.Empty(Queued(queue));
    }

    [Theory]
    [InlineData("athlete", "create")]
    [InlineData("activity", "update")]
    [InlineData("activity", "delete")]
    public async Task Other_events_are_ignored(string objectType, string aspect)
    {
        using var db = new TestDb();
        await db.AddUserAsync(Athlete);
        var (service, queue) = Build(db);

        await service.AcceptAsync(Event(objectType, aspect));

        Assert.Empty(Queued(queue));
        Assert.Equal(1, await db.Context().Users.CountAsync());
    }

    [Fact]
    public async Task A_retried_event_is_queued_once()
    {
        using var db = new TestDb();
        await db.AddUserAsync(Athlete);
        var (service, queue) = Build(db);

        await service.AcceptAsync(Event());
        await service.AcceptAsync(Event());

        Assert.Equal([(Athlete, 99L)], Queued(queue));
    }

    [Fact]
    public async Task Revoking_access_on_Strava_deletes_that_user_and_their_runs_but_nobody_else()
    {
        using var db = new TestDb();
        var leaver = await db.AddUserAsync(Athlete);
        var stayer = await db.AddUserAsync(1);
        foreach (var user in new[] { leaver, stayer })
            await new RunStore(db.Context()).SaveAsync(user.Id, new Run
            {
                ActivityId = 5,
                Name = "run",
                Songs = [new RunSong { Name = "song" }],
            });
        var (service, queue) = Build(db);

        await service.AcceptAsync(Deauthorisation());

        using var read = db.Context();
        Assert.Equal(stayer.Id, Assert.Single(await read.Users.ToListAsync()).Id);
        Assert.Equal(stayer.Id, Assert.Single(await read.Runs.ToListAsync()).UserId);
        Assert.Single(await read.RunSongs.ToListAsync());
        Assert.Empty(Queued(queue));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("anything")]
    public async Task An_athlete_update_that_is_not_a_revocation_deletes_nobody(string authorized)
    {
        using var db = new TestDb();
        await db.AddUserAsync(Athlete);
        var (service, _) = Build(db);

        await service.AcceptAsync(Deauthorisation(authorized));

        Assert.Equal(1, await db.Context().Users.CountAsync());
    }

    [Fact]
    public async Task Revoking_access_for_an_unknown_athlete_is_harmless()
    {
        using var db = new TestDb();
        await db.AddUserAsync(Athlete);
        var (service, _) = Build(db);

        await service.AcceptAsync(Deauthorisation(owner: 1));

        Assert.Equal(1, await db.Context().Users.CountAsync());
    }
}
