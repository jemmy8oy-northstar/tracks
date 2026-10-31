using System.Text;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

public class StravaDescriptionWriter(IStravaClient strava, ILogger<StravaDescriptionWriter> logger) : IStravaDescriptionWriter
{
    // Strava descriptions are plain text, so the delimiters have to be visible lines.
    public const string BlockStart = "── Tracks ──";
    public const string BlockEnd = "── end Tracks ──";

    public async Task WriteAsync(IUser user, IStravaActivity activity, IReadOnlyList<IRunSong> songs, CancellationToken cancellationToken = default)
    {
        if (!user.WriteSongsToStravaDescription)
            return;
        if (!user.StravaWriteGranted)
        {
            logger.LogInformation("Not writing songs into activity {ActivityId}: Strava has not granted activity:write; the runner must reconnect.", activity.Id);
            return;
        }

        // Nothing to say for a run with no songs, and an existing block is left as it was.
        if (songs.Count == 0)
            return;

        try
        {
            var existing = activity.Description ?? string.Empty;
            var updated = WithBlock(existing, Block(songs));
            if (updated == existing)
                return; // Already says exactly this: re-processing the activity must not write again.

            await strava.UpdateDescriptionAsync(user.Id, activity.Id, updated, cancellationToken);
            logger.LogInformation("Wrote {SongCount} songs into the description of activity {ActivityId}.", songs.Count, activity.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not write songs into the description of activity {ActivityId}; the run is kept regardless.", activity.Id);
        }
    }

    private static string Block(IReadOnlyList<IRunSong> songs)
    {
        var block = new StringBuilder().Append(BlockStart);
        for (var i = 0; i < songs.Count; i++)
            block.Append('\n').Append(i + 1).Append(". ").Append(songs[i].Name).Append(" - ").Append(string.Join(", ", songs[i].Artists));
        return block.Append('\n').Append(BlockEnd).ToString();
    }

    // Replaces only a Tracks block already in the text; otherwise appends one after what the runner wrote.
    private static string WithBlock(string existing, string block)
    {
        var start = existing.IndexOf(BlockStart, StringComparison.Ordinal);
        var end = start < 0 ? -1 : existing.IndexOf(BlockEnd, start, StringComparison.Ordinal);
        if (start >= 0 && end >= 0)
            return existing[..start] + block + existing[(end + BlockEnd.Length)..];

        return existing.Trim().Length == 0 ? block : existing.TrimEnd() + "\n\n" + block;
    }
}
