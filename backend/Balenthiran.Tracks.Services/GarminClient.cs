using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

/// <summary>
/// Reads Garmin's Personal API (BEH-SYNC-8). The key is traded for a token that lasts about a day;
/// it is kept until less than an hour of it remains. The key and token are never logged.
/// </summary>
public class GarminClient(HttpClient http, IGarminSettings settings, TimeProvider clock) : IGarminClient
{
    private static readonly TimeSpan RenewWithin = TimeSpan.FromHours(1);

    private readonly SemaphoreSlim gate = new(1, 1);
    private string? token;
    private DateTimeOffset tokenExpiresAt;

    public async Task<IReadOnlyList<IGarminRun>> SearchRunsAsync(DateOnly since, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/activitylist-service/activities/search/activities?start=0&limit=20&startDate={since:yyyy-MM-dd}";
        using var response = await GetAsync(url, "application/json", cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new UpstreamServiceException("Garmin's activity list was not JSON.", ex);
        }

        using (parsed)
        {
            if (parsed.RootElement.ValueKind != JsonValueKind.Array)
                throw new UpstreamServiceException("Garmin's activity list was not an array.");

            // Parsed defensively: an item missing a field is skipped, not fatal to the rest.
            return parsed.RootElement.EnumerateArray().Select(AsRun).OfType<IGarminRun>().ToList();
        }
    }

    public async Task<byte[]> DownloadAsync(long activityId, CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync($"{BaseUrl}/download-service/files/activity/{activityId}", "*/*", cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private string BaseUrl => settings.BaseUrl.TrimEnd('/');

    private static GarminRun? AsRun(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object
            || !item.TryGetProperty("activityId", out var id) || !id.TryGetInt64(out var activityId)
            || !item.TryGetProperty("startTimeGMT", out var start) || start.ValueKind != JsonValueKind.String
            || !DateTimeOffset.TryParseExact(start.GetString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var startedAt)
            || !item.TryGetProperty("activityType", out var type) || type.ValueKind != JsonValueKind.Object
            || !type.TryGetProperty("typeKey", out var key) || key.ValueKind != JsonValueKind.String
            || !(key.GetString() ?? string.Empty).Contains("running", StringComparison.Ordinal))
            return null;

        return new GarminRun { ActivityId = activityId, StartedAt = startedAt };
    }

    private async Task<HttpResponseMessage> GetAsync(string url, string accept, CancellationToken cancellationToken)
    {
        var jwt = await GetTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Headers.Accept.ParseAdd(accept);

        var response = await SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return response;

        var status = response.StatusCode;
        response.Dispose();
        if (status == HttpStatusCode.Unauthorized)
        {
            // Revoked: forget it so the next pass trades the key again.
            await DropTokenAsync(jwt, cancellationToken);
            throw new UpstreamServiceException("Garmin refused the token; it will be requested again.");
        }

        throw new UpstreamServiceException($"Garmin request failed with status {(int)status}.");
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (token is not null && tokenExpiresAt - clock.GetUtcNow() >= RenewWithin)
                return token;

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/personalapitoken-service/personal-api/personal-api-key/token");
            request.Headers.TryAddWithoutValidation("Authorization", $"ApiKey {settings.ApiKey}");
            request.Headers.Accept.ParseAdd("*/*");

            using var response = await SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new UpstreamServiceException($"Garmin token request failed with status {(int)response.StatusCode}.");

            var jwt = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            var expiresAt = ExpiryOf(jwt) ?? throw new UpstreamServiceException("Garmin's token carried no expiry.");
            token = jwt;
            tokenExpiresAt = expiresAt;
            return jwt;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task DropTokenAsync(string refused, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            // Only the token that was refused: another caller may already have minted a new one.
            if (token == refused)
                token = null;
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
            throw new UpstreamServiceException("Garmin was unreachable.", ex);
        }
    }

    /// <summary>The JWT's <c>exp</c> claim (unix seconds), or null if the token isn't a JWT or has none.</summary>
    private static DateTimeOffset? ExpiryOf(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds)
                    ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                    : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
