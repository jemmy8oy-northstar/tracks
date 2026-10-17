namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>The event body Strava POSTs to the webhook.</summary>
public interface IStravaWebhookEvent
{
    string? ObjectType { get; }
    long ObjectId { get; }
    string? AspectType { get; }
    long OwnerId { get; }

    /// <summary>What changed, e.g. <c>authorized: "false"</c> when an athlete revokes access. Values are JSON as received.</summary>
    IReadOnlyDictionary<string, object?>? Updates { get; }
}
