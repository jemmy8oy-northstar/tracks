namespace Balenthiran.Tracks.Abstractions.Services;

/// <summary>Hands activity ids from the webhook to the background worker, once per activity.</summary>
public interface IRunSyncQueue
{
    /// <summary>Queues the activity. Returns false if that activity id was already queued.</summary>
    bool TryEnqueue(long activityId);

    /// <summary>Forgets the activity id so a later retry from Strava can queue it again.</summary>
    void Release(long activityId);

    IAsyncEnumerable<long> ReadAllAsync(CancellationToken cancellationToken);
}
