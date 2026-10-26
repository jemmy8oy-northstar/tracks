using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class SpotifyPlay : ISpotifyPlay
{
    public string TrackId { get; set; } = string.Empty;
    public string TrackName { get; set; } = string.Empty;
    public IReadOnlyList<string> Artists { get; set; } = [];
    public string? AlbumArtUrl { get; set; }
    public int DurationMs { get; set; }
    public DateTimeOffset PlayedAt { get; set; }
    public string? ContextType { get; set; }
    public string? ContextUri { get; set; }
}
