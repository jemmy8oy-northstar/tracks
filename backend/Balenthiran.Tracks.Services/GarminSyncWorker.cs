using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

/// <summary>Polls Garmin for new runs (BEH-SYNC-8): once a minute after start, then every <c>Garmin:PollMinutes</c>. Idle until the key is configured.</summary>
public class GarminSyncWorker(IServiceScopeFactory scopes, IGarminSettings settings, ILogger<GarminSyncWorker> logger) : BackgroundService
{
    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.IsConfigured)
            return;

        var interval = TimeSpan.FromMinutes(Math.Max(1, settings.PollMinutes));
        try
        {
            await Task.Delay(StartDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IGarminSyncService>().SyncAsync(stoppingToken);
                }
                // Only shutdown ends the loop: anything else, a timeout's cancellation included, is one failed
                // pass. A BackgroundService that throws stops the whole host by default.
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "The Garmin sync pass failed.");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
