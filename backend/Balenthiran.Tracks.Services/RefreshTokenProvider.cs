using System.Collections.Concurrent;
using System.Text.Json;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Database;
using Balenthiran.Tracks.EntityModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Balenthiran.Tracks.Services;

/// <summary>
/// Exchanges each user's stored OAuth refresh token for an access token and keeps it, per user, until
/// shortly before it expires. A refresh token the provider rotates is written back to the user.
/// Subclasses supply only the request, how to read the reply and where the token lives on the user.
/// </summary>
public abstract class RefreshTokenProvider(HttpClient http, IServiceScopeFactory scopes, TimeProvider time)
{
    // Refresh a little early so a token never expires between being handed out and being used.
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<Guid, CachedToken> cache = new();

    protected abstract string ServiceName { get; }

    protected abstract string? GetRefreshToken(UserEntity user);

    protected abstract void SetRefreshToken(UserEntity user, string refreshToken);

    protected abstract HttpRequestMessage BuildRequest(string refreshToken);

    /// <summary>Reads the token reply; the expiry is relative to <paramref name="now"/>.</summary>
    protected abstract (string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt) ParseResponse(JsonElement body, DateTimeOffset now);

    public async Task<string> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // One gate per user: a slow exchange for one person never holds up another's.
        var entry = cache.GetOrAdd(userId, _ => new CachedToken());
        await entry.Gate.WaitAsync(cancellationToken);
        try
        {
            var now = time.GetUtcNow();
            if (entry.AccessToken is not null && now < entry.ExpiresAt - ExpirySkew)
                return entry.AccessToken;

            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new NotFoundException("That user does not exist.");
            var refreshToken = GetRefreshToken(user)
                ?? throw new NotFoundException($"{ServiceName} is not connected for this user.");

            using var request = BuildRequest(refreshToken);
            using var response = await SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new UpstreamServiceException($"{ServiceName} token refresh failed with status {(int)response.StatusCode}.");

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var parsed = ParseResponse(body.RootElement, now);

            // Both providers may rotate the refresh token; the old one may stop working, so keep the newest.
            if (!string.IsNullOrWhiteSpace(parsed.RefreshToken) && parsed.RefreshToken != refreshToken)
            {
                SetRefreshToken(user, parsed.RefreshToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            entry.AccessToken = parsed.AccessToken;
            entry.ExpiresAt = parsed.ExpiresAt;
            return entry.AccessToken;
        }
        finally
        {
            entry.Gate.Release();
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new UpstreamServiceException($"{ServiceName} token endpoint was unreachable.", ex);
        }
    }

    private sealed class CachedToken
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        public string? AccessToken { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }
    }
}
