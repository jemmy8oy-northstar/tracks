using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

/// <summary>Processes queued activities after the webhook has already replied to Strava.</summary>
public class RunSyncWorker(IRunSyncQueue queue, IServiceScopeFactory scopes, ILogger<RunSyncWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (athleteId, activityId) in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                // The processor reads and writes the database, so each activity gets its own scope.
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IRunSyncProcessor>().ProcessAsync(athleteId, activityId, stoppingToken);
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
