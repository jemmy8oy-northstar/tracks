using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IStravaClient
{
    /// <summary>Fetches one of the user's activities (GET /api/v3/activities/{id}).</summary>
    Task<IStravaActivity> GetActivityAsync(Guid userId, long activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// BEH-SHARE-2: replaces the activity's description (PUT /api/v3/activities/{id}). Needs the
    /// <c>activity:write</c> grant; any failure throws.
    /// </summary>
    Task UpdateDescriptionAsync(Guid userId, long activityId, string description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the activity's time, distance and smoothed-velocity streams
    /// (GET /api/v3/activities/{id}/streams). Null when the activity has no GPS data (404, or no
    /// time or distance stream); other failures throw.
    /// </summary>
    Task<IActivityStreams?> GetStreamsAsync(Guid userId, long activityId, CancellationToken cancellationToken = default);
}
