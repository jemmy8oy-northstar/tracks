using System.Threading.Channels;
using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.Services;

public class RunSyncQueue : IRunSyncQueue
{
    private readonly Channel<(long AthleteId, long ActivityId)> channel =
        Channel.CreateUnbounded<(long AthleteId, long ActivityId)>(new UnboundedChannelOptions { SingleReader = true });
    private readonly HashSet<long> seen = [];
    private readonly Lock gate = new();

    public bool TryEnqueue(long athleteId, long activityId)
    {
        lock (gate)
        {
            if (!seen.Add(activityId))
                return false;
        }

        return channel.Writer.TryWrite((athleteId, activityId));
    }

    public void Release(long activityId)
    {
        lock (gate)
            seen.Remove(activityId);
    }

    public IAsyncEnumerable<(long AthleteId, long ActivityId)> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
