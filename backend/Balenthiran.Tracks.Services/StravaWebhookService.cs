using System.Security.Cryptography;
using System.Text;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

public class StravaWebhookService(IStravaSettings settings, IRunSyncQueue queue, IUserService users, ILogger<StravaWebhookService> logger)
    : IStravaWebhookService
{
    public bool IsValidSubscription(string? mode, string? verifyToken)
    {
        // An unset token must never match, or an unconfigured app would accept any subscriber.
        if (mode != "subscribe" || string.IsNullOrEmpty(settings.VerifyToken) || verifyToken is null)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(settings.VerifyToken),
            Encoding.UTF8.GetBytes(verifyToken));
    }

    public async Task AcceptAsync(IStravaWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        if (IsDeauthorisation(webhookEvent))
        {
            // The athlete revoked us on Strava: the privacy-respecting thing is to forget them entirely.
            await users.DeleteByAthleteAsync(webhookEvent.OwnerId, cancellationToken);
            logger.LogInformation("Deleted the user whose Strava athlete {AthleteId} revoked access.", webhookEvent.OwnerId);
            return;
        }

        if (webhookEvent.ObjectType != "activity" || webhookEvent.AspectType != "create")
            return;

        // Strava tells us about every athlete who authorised the app, including ones we have since deleted.
        if (await users.FindByAthleteAsync(webhookEvent.OwnerId, cancellationToken) is null)
        {
            logger.LogInformation("Ignoring Strava event for activity {ActivityId}: its athlete is not a user.", webhookEvent.ObjectId);
            return;
        }

        if (!queue.TryEnqueue(webhookEvent.OwnerId, webhookEvent.ObjectId))
            logger.LogInformation("Strava activity {ActivityId} was already received.", webhookEvent.ObjectId);
    }

    private static bool IsDeauthorisation(IStravaWebhookEvent e) =>
        e.ObjectType == "athlete"
        && e.AspectType == "update"
        && e.Updates is not null
        && e.Updates.TryGetValue("authorized", out var authorized)
        && string.Equals(authorized?.ToString(), "false", StringComparison.OrdinalIgnoreCase);
}
