using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

/// <summary>Processes queued activities after the webhook has already replied to Strava.</summary>
public class RunSyncWorker(IRunSyncQueue queue, IRunSyncProcessor processor, ILogger<RunSyncWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var activityId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await processor.ProcessAsync(activityId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // One bad activity must not stop the worker; free the id so Strava can resend it.
                logger.LogError(ex, "Processing Strava activity {ActivityId} failed.", activityId);
                queue.Release(activityId);
            }
        }
    }
}
