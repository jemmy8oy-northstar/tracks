using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IStravaWebhookService
{
    /// <summary>True only for hub.mode=subscribe with the configured verify token.</summary>
    bool IsValidSubscription(string? mode, string? verifyToken);

    /// <summary>Queues the event if it is a new activity from the connected athlete; ignores everything else.</summary>
    void Accept(IStravaWebhookEvent webhookEvent);
}
