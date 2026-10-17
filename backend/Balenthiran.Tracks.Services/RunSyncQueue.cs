using System.Threading.Channels;
using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.Services;

public class RunSyncQueue : IRunSyncQueue
{
    private readonly Channel<long> channel = Channel.CreateUnbounded<long>(new UnboundedChannelOptions { SingleReader = true });
    private readonly HashSet<long> seen = [];
    private readonly Lock gate = new();

    public bool TryEnqueue(long activityId)
    {
        lock (gate)
        {
            if (!seen.Add(activityId))
                return false;
        }

        return channel.Writer.TryWrite(activityId);
    }

    public void Release(long activityId)
    {
        lock (gate)
            seen.Remove(activityId);
    }

    public IAsyncEnumerable<long> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
