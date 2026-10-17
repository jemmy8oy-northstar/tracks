namespace Balenthiran.Tracks.Abstractions.Services;

/// <summary>Connects (or disconnects) a signed-in user's Spotify account.</summary>
public interface ISpotifyAuthService
{
    string BuildAuthorizeUrl(string redirectUri, string state);

    /// <summary>Exchanges the code and stores the refresh token on the user. <paramref name="redirectUri"/> must match the authorize request.</summary>
    Task ConnectAsync(Guid userId, string code, string redirectUri, CancellationToken cancellationToken = default);

    Task DisconnectAsync(Guid userId, CancellationToken cancellationToken = default);
}
