namespace Balenthiran.Tracks.Abstractions.Services;

public interface IRunSyncProcessor
{
    /// <summary>Fetches the activity, finds the songs played during it and stores the run. Non-runs are ignored.</summary>
    Task ProcessAsync(long activityId, CancellationToken cancellationToken = default);
}
