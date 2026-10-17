using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class SpotifySettings : ISpotifySettings
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}
