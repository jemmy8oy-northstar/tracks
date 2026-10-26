using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IRunHighlights
{
    /// <summary>
    /// Picks the highlighted songs of a run from its GPS streams. Song spans are
    /// [OffsetSeconds, OffsetSeconds + length] clipped to the run. Top speed: the maximum of the
    /// velocity smoothed over a 5-sample centred window, so one GPS jump cannot win; the song
    /// playing at that moment. Fastest pace: distance covered inside a song's span over the span,
    /// skipping songs that cover under 30 s of the run. Null streams (no GPS), or streams missing
    /// what a pick needs, name nothing.
    /// </summary>
    IRunHighlightPicks Pick(IReadOnlyList<IRunSong> songs, int elapsedSeconds, IActivityStreams? streams);
}
