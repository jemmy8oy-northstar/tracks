namespace Balenthiran.Tracks.Abstractions.Services;

public interface ISpotifyTokenProvider
{
    /// <summary>A valid access token, refreshed from the configured refresh token when needed.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
