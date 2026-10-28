using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IRunUploadService
{
    /// <summary>
    /// Reads the run in a Garmin FIT file (BEH-SYNC-7), pairs it with the songs Spotify says played during it and keeps it
    /// for the user, replacing any run of theirs with the same start time. Returns the run kept. The file itself is not kept.
    /// Throws <c>ValidationException</c> for a file that is not a run, and for a user who has not connected Spotify.
    /// </summary>
    Task<IRun> UploadAsync(Guid userId, Stream file, CancellationToken cancellationToken = default);
}
