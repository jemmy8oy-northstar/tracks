using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IGarminSyncService
{
    /// <summary>
    /// BEH-SYNC-8: imports the configured user's new Garmin runs as if uploaded (BEH-SYNC-7). Makes
    /// no HTTP call at all until the key is configured.
    /// </summary>
    Task<IGarminSyncResult> SyncAsync(CancellationToken cancellationToken = default);
}
