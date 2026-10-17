using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunSyncQueueTests
{
    private static async Task<List<(long AthleteId, long ActivityId)>> Drain(RunSyncQueue queue, int count)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var read = new List<(long AthleteId, long ActivityId)>();
        await foreach (var item in queue.ReadAllAsync(cts.Token))
        {
            read.Add(item);
            if (read.Count == count)
                break;
        }

        return read;
    }

    [Fact]
    public async Task An_activity_queued_twice_is_delivered_once_with_its_athlete()
    {
        var queue = new RunSyncQueue();

        Assert.True(queue.TryEnqueue(1, 7));
        Assert.False(queue.TryEnqueue(1, 7));
        Assert.True(queue.TryEnqueue(2, 8));

        Assert.Equal([(1L, 7L), (2L, 8L)], await Drain(queue, 2));
    }

    [Fact]
    public async Task A_released_activity_can_be_queued_again()
    {
        var queue = new RunSyncQueue();
        queue.TryEnqueue(1, 7);
        await Drain(queue, 1);

        queue.Release(7);

        Assert.True(queue.TryEnqueue(1, 7));
    }
}
