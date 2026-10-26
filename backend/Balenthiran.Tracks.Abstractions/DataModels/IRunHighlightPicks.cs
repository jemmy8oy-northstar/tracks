namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>Which songs of a run earned a highlight; an index into the song list, or null when no song did.</summary>
public interface IRunHighlightPicks
{
    /// <summary>The song playing at the run's top (smoothed) speed.</summary>
    int? TopSpeedIndex { get; }

    /// <summary>The song with the fastest average pace.</summary>
    int? FastestPaceIndex { get; }
}
