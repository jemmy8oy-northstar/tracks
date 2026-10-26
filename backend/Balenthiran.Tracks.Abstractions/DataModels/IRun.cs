namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A finished run kept together with the songs played during it.</summary>
public interface IRun
{
    long ActivityId { get; }
    string Name { get; }
    string SportType { get; }
    DateTimeOffset StartedAt { get; }
    int ElapsedSeconds { get; }
    IReadOnlyList<IRunSong> Songs { get; }

    /// <summary>What most of the songs came from (BEH-HIGH-2): playlist, album, artist..., or "mixed" when none held a majority. Null when unknown (a run synced earlier, or no song had a context).</summary>
    string? ContextType { get; }

    /// <summary>The name of that context; null when mixed, unknown, or Spotify would not tell us.</summary>
    string? ContextName { get; }
}
