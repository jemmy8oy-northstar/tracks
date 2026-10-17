namespace Balenthiran.Tracks.Abstractions.Services;

public interface ISpotifyTokenProvider
{
    /// <summary>A valid Spotify access token for the user, refreshed from their stored refresh token when needed.</summary>
    Task<string> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default);
}
