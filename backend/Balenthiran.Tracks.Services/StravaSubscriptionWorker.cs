using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

/// <summary>Ensures the Strava push subscription once the app is serving, retrying a failure a few times.</summary>
public class StravaSubscriptionWorker(
    IStravaSubscriptionService subscription,
    IHostApplicationLifetime lifetime,
    ILogger<StravaSubscriptionWorker> logger,
    IReadOnlyList<TimeSpan> retryDelays) : BackgroundService
{
    // On a first deploy the ingress may not route to this pod yet, and Strava's handshake needs it to.
    public static readonly IReadOnlyList<TimeSpan> DefaultRetryDelays =
        [TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var started = new TaskCompletionSource();
        using (lifetime.ApplicationStarted.Register(() => started.TrySetResult()))
        using (stoppingToken.Register(() => started.TrySetCanceled()))
        {
            try
            {
                await started.Task;
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        try
        {
            for (var attempt = 0; ; attempt++)
            {
                if (await subscription.EnsureAsync(stoppingToken) != "failed")
                    return;
                if (attempt >= retryDelays.Count)
                {
                    logger.LogError("Gave up on the Strava push subscription; no run will be noticed until it exists.");
                    return;
                }

                await Task.Delay(retryDelays[attempt], stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
