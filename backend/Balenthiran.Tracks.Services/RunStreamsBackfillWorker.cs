using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

/// <summary>
/// Works through runs whose streams were never read (BEH-SYNC-6), a few at a time. One Strava call
/// every 20 s is at most 45 per 15 minutes, under half of Strava's 100, leaving room for new runs.
/// </summary>
public class RunStreamsBackfillWorker(IServiceScopeFactory scopes, ILogger<RunStreamsBackfillWorker> logger) : BackgroundService
{
    private const int BatchSize = 5;
    private static readonly TimeSpan Spacing = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan StartDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Lives for the process: a run that failed is left alone until the next deploy.
        var skip = new HashSet<long>();
        try
        {
            await Task.Delay(StartDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                int filled;
                try
                {
                    using var scope = scopes.CreateScope();
                    filled = await scope.ServiceProvider.GetRequiredService<IRunStreamsBackfill>()
                        .RunOnceAsync(BatchSize, Spacing, skip, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "The streams backfill pass failed.");
                    filled = 0;
                }

                if (filled > 0)
                    logger.LogInformation("Backfilled streams for {Count} runs.", filled);

                await Task.Delay(filled > 0 ? Spacing : IdleDelay, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
