using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IStravaClient
{
    /// <summary>Fetches one of the user's activities (GET /api/v3/activities/{id}).</summary>
    Task<IStravaActivity> GetActivityAsync(Guid userId, long activityId, CancellationToken cancellationToken = default);
}
