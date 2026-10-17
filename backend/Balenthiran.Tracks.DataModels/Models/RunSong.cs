using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class RunSong : IRunSong
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<string> Artists { get; set; } = [];
    public string? AlbumArtUrl { get; set; }
    public DateTimeOffset PlayedFrom { get; set; }
    public DateTimeOffset PlayedTo { get; set; }
    public int OffsetSeconds { get; set; }
}
