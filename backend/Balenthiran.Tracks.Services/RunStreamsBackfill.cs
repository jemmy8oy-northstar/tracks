using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

/// <summary>
/// Gives runs kept before a feature existed their highlights and route (BEH-SYNC-6). Unlike their
/// songs, which Spotify forgets, both come from Strava's streams, which Strava keeps.
/// </summary>
public class RunStreamsBackfill(
    IStravaSettings stravaSettings,
    IStravaClient strava,
    IRunHighlights highlights,
    IRunStore store,
    ILogger<RunStreamsBackfill> logger) : IRunStreamsBackfill
{
    public async Task<int> RunOnceAsync(int limit, TimeSpan spacing, ISet<long> skip, CancellationToken cancellationToken = default)
    {
        if (!stravaSettings.IsConfigured)
            return 0;

        var filled = 0;
        var runs = await store.GetUnreadStreamsAsync(limit, skip.ToList(), cancellationToken);
        for (var i = 0; i < runs.Count; i++)
        {
            var (userId, run) = runs[i];
            if (i > 0 && spacing > TimeSpan.Zero)
                await Task.Delay(spacing, cancellationToken);

            try
            {
                // A 404 (no GPS, or Strava no longer has it) reads as null streams: tried once, and kept as it is.
                var streams = await strava.GetStreamsAsync(userId, run.ActivityId, cancellationToken);
                var songs = RunRoute.WithPicks(run.Songs, highlights.Pick(run.Songs, run.ElapsedSeconds, streams));
                await store.SaveStreamsAsync(userId, run.ActivityId, songs, RunRoute.FromStreams(streams), cancellationToken);
                filled++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Backfill skipped run {ActivityId}: its streams could not be read.", run.ActivityId);
                skip.Add(run.ActivityId);
            }
        }

        return filled;
    }
}
