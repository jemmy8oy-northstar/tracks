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
}
