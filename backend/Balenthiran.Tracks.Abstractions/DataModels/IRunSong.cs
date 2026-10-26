namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A song the runner listened to during a run.</summary>
public interface IRunSong
{
    string Name { get; }
    IReadOnlyList<string> Artists { get; }
    string? AlbumArtUrl { get; }
    DateTimeOffset PlayedFrom { get; }
    DateTimeOffset PlayedTo { get; }

    /// <summary>Seconds from the run's start to when the song started; negative if it began before the run.</summary>
    int OffsetSeconds { get; }

    /// <summary>The song playing at the run's top speed (BEH-HIGH-3). False for a run with no GPS.</summary>
    bool IsTopSpeed { get; }

    /// <summary>The song with the fastest average pace over the run (BEH-HIGH-4). False for a run with no GPS.</summary>
    bool IsFastestPace { get; }

    /// <summary>The kind of Spotify context the song was played from (BEH-HIGH-2): playlist, album, artist, show...; null for a queue. Only set while syncing, never stored per song.</summary>
    string? ContextType { get; }

    /// <summary>The Spotify uri of that context (spotify:playlist:...); null for a queue. Only set while syncing.</summary>
    string? ContextUri { get; }
}
