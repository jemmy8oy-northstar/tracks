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

    /// <summary>The route as JSON [[lat, lng, seconds], ...] (BEH-MAP-2); null when the run has no GPS or its streams are not read yet.</summary>
    public string? Route { get; set; }

    /// <summary>When Strava's streams were read for highlights and the route; null for runs kept before BEH-SYNC-6, which the backfill picks up.</summary>
    public DateTimeOffset? StreamsReadAt { get; set; }

    public List<RunSongEntity> Songs { get; set; } = [];
}
