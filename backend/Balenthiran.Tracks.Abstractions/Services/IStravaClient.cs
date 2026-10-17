using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IStravaClient
{
    /// <summary>Fetches one activity (GET /api/v3/activities/{id}).</summary>
    Task<IStravaActivity> GetActivityAsync(long activityId, CancellationToken cancellationToken = default);
}
