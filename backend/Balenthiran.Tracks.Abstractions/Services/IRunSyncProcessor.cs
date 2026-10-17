namespace Balenthiran.Tracks.Abstractions.Services;

public interface IRunSyncProcessor
{
    /// <summary>
    /// Fetches the athlete's activity, finds the songs played during it and stores the run for their user.
    /// Non-runs, unknown athletes and users without Spotify connected are ignored.
    /// </summary>
    Task ProcessAsync(long athleteId, long activityId, CancellationToken cancellationToken = default);
}
