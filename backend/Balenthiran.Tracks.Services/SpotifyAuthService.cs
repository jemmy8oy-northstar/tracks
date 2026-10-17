using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.Database;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Services;

public class SpotifyAuthService(HttpClient http, AppDbContext db, ISpotifySettings settings) : ISpotifyAuthService
{
    public string BuildAuthorizeUrl(string redirectUri, string state) =>
        QueryHelpers.AddQueryString("https://accounts.spotify.com/authorize", new Dictionary<string, string?>
        {
            ["client_id"] = settings.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "user-read-recently-played",
            ["state"] = state,
        });

    public async Task ConnectAsync(Guid userId, string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.ClientId}:{settings.ClientSecret}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", credentials) },
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
            }),
        };

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new UpstreamServiceException("Spotify token endpoint was unreachable.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new UpstreamServiceException($"Spotify token exchange failed with status {(int)response.StatusCode}.");

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var refreshToken = body.RootElement.GetProperty("refresh_token").GetString()!;

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new UnauthorizedException("This account no longer exists.");
            user.SpotifyRefreshToken = refreshToken;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DisconnectAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("This account no longer exists.");
        user.SpotifyRefreshToken = null;
        await db.SaveChangesAsync(cancellationToken);
    }
}
