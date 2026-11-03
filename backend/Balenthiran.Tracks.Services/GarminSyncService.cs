using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

public class GarminSyncService(
    IGarminSettings settings,
    IGarminClient garmin,
    IRunStore store,
    IRunUploadService uploads,
    TimeProvider clock,
    ILogger<GarminSyncService> logger) : IGarminSyncService
{
    /// <summary>Looks back this far each pass, so a pass that failed (or a watch that synced late) is caught by the next.</summary>
    private static readonly TimeSpan LookBack = TimeSpan.FromDays(2);

    public async Task<IGarminSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        var result = new GarminSyncResult();
        if (!settings.IsConfigured || settings.ImportUserId is not { } userId)
            return result;

        var since = DateOnly.FromDateTime(clock.GetUtcNow().Subtract(LookBack).UtcDateTime);
        foreach (var run in await garmin.SearchRunsAsync(since, cancellationToken))
        {
            if (await store.ExistsAtAsync(userId, run.StartedAt, cancellationToken))
            {
                result.Skipped++;
                continue;
            }

            try
            {
                var file = await garmin.DownloadAsync(run.ActivityId, cancellationToken);
                await uploads.UploadAsync(userId, new MemoryStream(file), cancellationToken);
                result.Imported++;
            }
            catch (ValidationException ex)
            {
                // This file's problem, not the rest's: say so and carry on.
                logger.LogWarning("Skipped Garmin activity {ActivityId}: {Reason}", run.ActivityId, ex.Message);
                result.Skipped++;
            }
        }

        if (result.Imported > 0)
            logger.LogInformation("Imported {Count} runs from Garmin.", result.Imported);
        return result;
    }
}
