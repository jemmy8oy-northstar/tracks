using System.Security.Cryptography;
using System.Text;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

public class StravaWebhookService(IStravaSettings settings, IRunSyncQueue queue, ILogger<StravaWebhookService> logger)
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

    public void Accept(IStravaWebhookEvent webhookEvent)
    {
        if (webhookEvent.ObjectType != "activity" || webhookEvent.AspectType != "create")
            return;

        if (settings.AthleteId is null)
        {
            logger.LogWarning("Ignoring Strava event for activity {ActivityId}: no athlete id is configured.", webhookEvent.ObjectId);
            return;
        }

        if (webhookEvent.OwnerId != settings.AthleteId)
            return;

        if (!queue.TryEnqueue(webhookEvent.ObjectId))
            logger.LogInformation("Strava activity {ActivityId} was already received.", webhookEvent.ObjectId);
    }
}
