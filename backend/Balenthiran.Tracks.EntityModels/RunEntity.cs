namespace Balenthiran.Tracks.EntityModels;

public class RunEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public long ActivityId { get; set; }

    public required string Name { get; set; }

    public required string SportType { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public int ElapsedSeconds { get; set; }

    /// <summary>What most of the run's songs came from: "playlist", "album", "artist", ..., "mixed" when no context held a majority; null when nothing is known (runs synced before BEH-HIGH-2, or no song had a context).</summary>
    public string? ContextType { get; set; }

    /// <summary>The name of that context; null when mixed, unknown, or Spotify would not tell us.</summary>
    public string? ContextName { get; set; }

    public List<RunSongEntity> Songs { get; set; } = [];
}
