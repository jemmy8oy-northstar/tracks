using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunSyncQueueTests
{
    private static async Task<List<long>> Drain(RunSyncQueue queue, int count)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var read = new List<long>();
        await foreach (var id in queue.ReadAllAsync(cts.Token))
        {
            read.Add(id);
            if (read.Count == count)
                break;
        }

        return read;
    }

    [Fact]
    public async Task An_activity_queued_twice_is_delivered_once()
    {
        var queue = new RunSyncQueue();

        Assert.True(queue.TryEnqueue(7));
        Assert.False(queue.TryEnqueue(7));
        Assert.True(queue.TryEnqueue(8));

        Assert.Equal([7L, 8L], await Drain(queue, 2));
    }

    [Fact]
    public async Task A_released_activity_can_be_queued_again()
    {
        var queue = new RunSyncQueue();
        queue.TryEnqueue(7);
        await Drain(queue, 1);

        queue.Release(7);

        Assert.True(queue.TryEnqueue(7));
    }
}
