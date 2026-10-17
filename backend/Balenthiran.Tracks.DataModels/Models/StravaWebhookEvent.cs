using System.Text.Json.Serialization;
using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class StravaWebhookEvent : IStravaWebhookEvent
{
    [JsonPropertyName("object_type")]
    public string? ObjectType { get; set; }

    [JsonPropertyName("object_id")]
    public long ObjectId { get; set; }

    [JsonPropertyName("aspect_type")]
    public string? AspectType { get; set; }

    [JsonPropertyName("owner_id")]
    public long OwnerId { get; set; }

    [JsonPropertyName("updates")]
    public Dictionary<string, object?>? Updates { get; set; }

    IReadOnlyDictionary<string, object?>? IStravaWebhookEvent.Updates => Updates;
}
