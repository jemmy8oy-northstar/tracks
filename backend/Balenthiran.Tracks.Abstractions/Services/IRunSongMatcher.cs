using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IRunSongMatcher
{
    /// <summary>
    /// The songs whose play overlaps the run, ordered by when they started. A play spans
    /// [PlayedAt - duration, PlayedAt]; the overlap is strict, so a song that only touches the
    /// run's start or end at a single instant does not count.
    /// </summary>
    IReadOnlyList<IRunSong> Match(DateTimeOffset runStart, TimeSpan elapsed, IEnumerable<ISpotifyPlay> plays);
}
