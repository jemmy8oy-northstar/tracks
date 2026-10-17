using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

/// <summary>Sign in with Strava: the Strava athlete is the account.</summary>
public interface IStravaAuthService
{
    string BuildAuthorizeUrl(string redirectUri, string state);

    /// <summary>
    /// Exchanges the authorisation code and creates the user, or updates the refresh token of the existing one.
    /// <paramref name="grantedScope"/> is the <c>scope</c> Strava echoed back; a grant without activity access is refused.
    /// </summary>
    Task<IUser> SignInAsync(string code, string? grantedScope, CancellationToken cancellationToken = default);
}
