using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class GarminSettings : IGarminSettings
{
    public string? ApiKey { get; set; }
    public string? UserId { get; set; }
    public string BaseUrl { get; set; } = "https://personal.connectapi.garmin.com";
    public int PollMinutes { get; set; } = 15;

    public Guid? ImportUserId => Guid.TryParse(UserId, out var id) ? id : null;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && ImportUserId is not null;
}
