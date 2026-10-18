using System.Text.Json;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

public class StravaSubscriptionService(
    HttpClient http,
    IStravaSettings settings,
    string? publicBaseUrl,
    ILogger<StravaSubscriptionService> logger) : IStravaSubscriptionService
{
    private const string Endpoint = "https://www.strava.com/api/v3/push_subscriptions";

    public string State { get; private set; } = "pending";

    public async Task<string> EnsureAsync(CancellationToken cancellationToken = default)
    {
        // Strava echoes the verify token in its handshake, and the callback must be our public URL.
        if (!settings.IsConfigured || string.IsNullOrWhiteSpace(settings.VerifyToken) || string.IsNullOrWhiteSpace(publicBaseUrl))
            return State = "not-configured";

        var callback = publicBaseUrl.TrimEnd('/') + "/api/strava/webhook";
        try
        {
            var existing = await GetCallbackUrlsAsync(cancellationToken);
            if (existing.Contains(callback))
                return State = "subscribed";
            if (existing.Count > 0)
            {
                logger.LogWarning("Strava already has a push subscription for {Existing}, so ours for {Callback} was not created.", existing[0], callback);
                return State = "other-callback";
            }

            // Strava calls our handshake route before it answers this, so the app must already be serving.
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = settings.ClientId!,
                ["client_secret"] = settings.ClientSecret!,
                ["callback_url"] = callback,
                ["verify_token"] = settings.VerifyToken,
            });
            using var response = await http.PostAsync(Endpoint, form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Strava refused the push subscription with status {Status}: {Body}",
                    (int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
                return State = "failed";
            }

            logger.LogInformation("Created the Strava push subscription for {Callback}.", callback);
            return State = "subscribed";
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Strava was unreachable while checking the push subscription.");
            return State = "failed";
        }
    }

    private async Task<List<string>> GetCallbackUrlsAsync(CancellationToken cancellationToken)
    {
        // Strava takes the app credentials as query parameters here, so this URL must never be logged.
        var url = $"{Endpoint}?client_id={Uri.EscapeDataString(settings.ClientId!)}&client_secret={Uri.EscapeDataString(settings.ClientSecret!)}";
        using var response = await http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Listing Strava push subscriptions failed with status {(int)response.StatusCode}.");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return body.RootElement.EnumerateArray()
            .Select(s => s.TryGetProperty("callback_url", out var c) ? c.GetString() : null)
            .OfType<string>()
            .ToList();
    }
}
