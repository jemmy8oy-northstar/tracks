using System.Text.Json;
using Balenthiran.Tracks.Abstractions.Exceptions;

namespace Balenthiran.Tracks.Services;

/// <summary>
/// Exchanges a configured OAuth refresh token for an access token and keeps it until shortly
/// before it expires. Subclasses supply only the request and how to read the reply.
/// </summary>
public abstract class RefreshTokenProvider(HttpClient http, TimeProvider time)
{
    // Refresh a little early so a token never expires between being handed out and being used.
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim gate = new(1, 1);
    private string? accessToken;
    private string? refreshToken;
    private DateTimeOffset expiresAt;

    protected abstract string ServiceName { get; }

    /// <summary>The refresh token from configuration, used until the provider hands out a newer one.</summary>
    protected abstract string ConfiguredRefreshToken { get; }

    protected abstract HttpRequestMessage BuildRequest(string refreshToken);

    /// <summary>Reads the token reply; the expiry is relative to <paramref name="now"/>.</summary>
    protected abstract (string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt) ParseResponse(JsonElement body, DateTimeOffset now);

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var now = time.GetUtcNow();
            if (accessToken is not null && now < expiresAt - ExpirySkew)
                return accessToken;

            using var request = BuildRequest(refreshToken ?? ConfiguredRefreshToken);
            using var response = await SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new UpstreamServiceException($"{ServiceName} token refresh failed with status {(int)response.StatusCode}.");

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var parsed = ParseResponse(body.RootElement, now);

            accessToken = parsed.AccessToken;
            expiresAt = parsed.ExpiresAt;
            // Both providers may rotate the refresh token; keep the newest one for the next exchange.
            if (!string.IsNullOrWhiteSpace(parsed.RefreshToken))
                refreshToken = parsed.RefreshToken;
            return accessToken;
        }
        finally
        {
            gate.Release();
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
}
