using System.Collections.Concurrent;
using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>Stands in for the real processor so the HTTP tests see what the worker was handed.</summary>
public sealed class RecordingProcessor : IRunSyncProcessor
{
    private readonly BlockingCollection<long> processed = [];

    public Task ProcessAsync(long activityId, CancellationToken cancellationToken = default)
    {
        processed.Add(activityId, cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>Waits for the next processed activity; null if none arrives in time.</summary>
    public long? Next(TimeSpan timeout) => processed.TryTake(out var id, timeout) ? id : null;
}
