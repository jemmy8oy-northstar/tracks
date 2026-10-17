using System.Text.Json;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.EntityModels;
using Microsoft.Extensions.DependencyInjection;

namespace Balenthiran.Tracks.Services;

public class StravaTokenProvider(HttpClient http, IServiceScopeFactory scopes, IStravaSettings settings, TimeProvider time)
    : RefreshTokenProvider(http, scopes, time), IStravaTokenProvider
{
    protected override string ServiceName => "Strava";

    protected override string? GetRefreshToken(UserEntity user) => user.StravaRefreshToken;

    protected override void SetRefreshToken(UserEntity user, string refreshToken) => user.StravaRefreshToken = refreshToken;

    protected override HttpRequestMessage BuildRequest(string refreshToken) =>
        new(HttpMethod.Post, "https://www.strava.com/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = settings.ClientId ?? string.Empty,
                ["client_secret"] = settings.ClientSecret ?? string.Empty,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            }),
        };

    protected override (string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt) ParseResponse(JsonElement body, DateTimeOffset now)
    {
        // Strava reports an absolute expiry as unix seconds.
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(body.GetProperty("expires_at").GetInt64());
        var refreshToken = body.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() : null;
        return (body.GetProperty("access_token").GetString()!, refreshToken, expiresAt);
    }
}
