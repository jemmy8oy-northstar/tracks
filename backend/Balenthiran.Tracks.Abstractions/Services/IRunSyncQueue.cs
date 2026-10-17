namespace Balenthiran.Tracks.Abstractions.Services;

/// <summary>Hands activities from the webhook to the background worker, once per activity.</summary>
public interface IRunSyncQueue
{
    /// <summary>Queues the athlete's activity. Returns false if that activity id was already queued.</summary>
    bool TryEnqueue(long athleteId, long activityId);

    /// <summary>Forgets the activity id so a later retry from Strava can queue it again.</summary>
    void Release(long activityId);

    IAsyncEnumerable<(long AthleteId, long ActivityId)> ReadAllAsync(CancellationToken cancellationToken);
}
