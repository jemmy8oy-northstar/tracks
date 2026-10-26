using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

public class RunSongMatcher : IRunSongMatcher
{
    public IReadOnlyList<IRunSong> Match(DateTimeOffset runStart, TimeSpan elapsed, IEnumerable<ISpotifyPlay> plays)
    {
        var runEnd = runStart + elapsed;

        return plays
            // played_at is when the play ended, so it started one track-length earlier.
            .Select(play => (Play: play, From: play.PlayedAt - TimeSpan.FromMilliseconds(play.DurationMs)))
            .Where(p => p.From < runEnd && p.Play.PlayedAt > runStart)
            .OrderBy(p => p.From)
            .Select(p => (IRunSong)new RunSong
            {
                Name = p.Play.TrackName,
                Artists = p.Play.Artists,
                AlbumArtUrl = p.Play.AlbumArtUrl,
                PlayedFrom = p.From,
                PlayedTo = p.Play.PlayedAt,
                OffsetSeconds = (int)(p.From - runStart).TotalSeconds,
                ContextType = p.Play.ContextType,
                ContextUri = p.Play.ContextUri,
            })
            .ToList();
    }
}
