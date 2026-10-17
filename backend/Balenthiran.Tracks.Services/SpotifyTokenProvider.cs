using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.Services;

public class SpotifyTokenProvider(HttpClient http, ISpotifySettings settings, TimeProvider time)
    : RefreshTokenProvider(http, time), ISpotifyTokenProvider
{
    protected override string ServiceName => "Spotify";

    protected override string ConfiguredRefreshToken => settings.RefreshToken ?? string.Empty;

    protected override HttpRequestMessage BuildRequest(string refreshToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.ClientId}:{settings.ClientSecret}"));
        return new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", credentials) },
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            }),
        };
    }

    protected override (string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt) ParseResponse(JsonElement body, DateTimeOffset now)
    {
        // Spotify reports a lifetime in seconds rather than an absolute expiry.
        var expiresAt = now.AddSeconds(body.GetProperty("expires_in").GetInt32());
        var refreshToken = body.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null;
        return (body.GetProperty("access_token").GetString()!, refreshToken, expiresAt);
    }
}
