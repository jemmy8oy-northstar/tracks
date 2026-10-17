using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class StravaWebhookServiceTests
{
    private const long Athlete = 4242;

    private static (StravaWebhookService Service, RunSyncQueue Queue) Build(string? verifyToken = "secret", long? athlete = Athlete)
    {
        var queue = new RunSyncQueue();
        var settings = new StravaSettings { VerifyToken = verifyToken, AthleteId = athlete };
        return (new StravaWebhookService(settings, queue, NullLogger<StravaWebhookService>.Instance), queue);
    }

    private static StravaWebhookEvent Event(string objectType = "activity", string aspect = "create", long owner = Athlete, long id = 99) =>
        new() { ObjectType = objectType, AspectType = aspect, OwnerId = owner, ObjectId = id };

    private static List<long> Queued(RunSyncQueue queue)
    {
        // The reader blocks once the queue is empty, so a short timeout marks the end of what was queued.
        var queued = new List<long>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        try
        {
            foreach (var id in queue.ReadAllAsync(cts.Token).ToBlockingEnumerable(cts.Token))
                queued.Add(id);
        }
        catch (OperationCanceledException)
        {
        }

        return queued;
    }

    [Fact]
    public void Subscribe_with_the_configured_token_is_valid()
    {
        Assert.True(Build().Service.IsValidSubscription("subscribe", "secret"));
    }

    [Theory]
    [InlineData("subscribe", "wrong")]
    [InlineData("subscribe", null)]
    [InlineData("unsubscribe", "secret")]
    [InlineData(null, "secret")]
    public void Anything_else_is_refused(string? mode, string? token)
    {
        Assert.False(Build().Service.IsValidSubscription(mode, token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void With_no_token_configured_nothing_is_ever_valid(string? configured)
    {
        Assert.False(Build(verifyToken: configured).Service.IsValidSubscription("subscribe", configured));
    }

    [Fact]
    public void A_new_activity_from_the_connected_athlete_is_queued()
    {
        var (service, queue) = Build();

        service.Accept(Event());

        Assert.Equal([99L], Queued(queue));
    }

    [Theory]
    [InlineData("athlete", "create", Athlete)]
    [InlineData("activity", "update", Athlete)]
    [InlineData("activity", "delete", Athlete)]
    [InlineData("activity", "create", 1L)]
    public void Other_events_are_ignored(string objectType, string aspect, long owner)
    {
        var (service, queue) = Build();

        service.Accept(Event(objectType, aspect, owner));

        Assert.Empty(Queued(queue));
    }

    [Fact]
    public void With_no_athlete_configured_events_are_ignored()
    {
        var (service, queue) = Build(athlete: null);

        service.Accept(Event());

        Assert.Empty(Queued(queue));
    }

    [Fact]
    public void A_retried_event_is_queued_once()
    {
        var (service, queue) = Build();

        service.Accept(Event());
        service.Accept(Event());

        Assert.Equal([99L], Queued(queue));
    }
}
