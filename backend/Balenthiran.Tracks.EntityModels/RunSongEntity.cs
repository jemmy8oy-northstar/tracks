namespace Balenthiran.Tracks.EntityModels;

public class RunSongEntity
{
    public Guid Id { get; set; }

    public Guid RunId { get; set; }

    /// <summary>Order within the run (0-based).</summary>
    public int Position { get; set; }

    public required string Name { get; set; }

    public List<string> Artists { get; set; } = [];

    public string? AlbumArtUrl { get; set; }

    public DateTimeOffset PlayedFrom { get; set; }

    public DateTimeOffset PlayedTo { get; set; }

    public int OffsetSeconds { get; set; }
}
