namespace Balenthiran.Tracks.Abstractions.Services;

public interface IStravaTokenProvider
{
    /// <summary>A valid Strava access token for the user, refreshed from their stored refresh token when needed.</summary>
    Task<string> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default);
}
