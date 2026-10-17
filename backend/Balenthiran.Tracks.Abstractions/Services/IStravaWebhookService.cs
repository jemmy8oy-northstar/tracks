using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IStravaWebhookService
{
    /// <summary>True only for hub.mode=subscribe with the configured verify token.</summary>
    bool IsValidSubscription(string? mode, string? verifyToken);

    /// <summary>
    /// Queues a new activity from a known athlete, and deletes a user who revoked access on Strava.
    /// Everything else — including events for athletes who never signed in — is ignored.
    /// </summary>
    Task AcceptAsync(IStravaWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
