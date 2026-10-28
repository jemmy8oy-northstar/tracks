using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

public class RunUploadService(
    IUserService users,
    IFitFileReader reader,
    ISpotifyClient spotify,
    IRunSongMatcher matcher,
    IRunContextNamer contextNamer,
    IRunHighlights highlights,
    IRunStore store,
    ILogger<RunUploadService> logger) : IRunUploadService
{
    /// <summary>Strava's ids are positive, so an upload takes the negative of its start (unix seconds): stable, and never one of theirs.</summary>
    public static long ActivityIdOf(DateTimeOffset startedAt) => -startedAt.ToUnixTimeSeconds();

    public async Task<IRun> UploadAsync(Guid userId, Stream file, CancellationToken cancellationToken = default)
    {
        // Read the file first: a bad one is the runner's to fix whether or not Spotify is connected.
        var fit = reader.Read(file);
        var user = await users.GetSignedInAsync(userId, cancellationToken);
        if (!user.SpotifyConnected)
            throw new ValidationException("Connect Spotify first: songs can't be matched to a run without it.");

        // Same as a Strava run (BEH-SYNC-1): recently-played holds the last 50 plays, matched by the run's start and end.
        var plays = await spotify.GetRecentlyPlayedAsync(user.Id, fit.StartedAt, cancellationToken);
        var songs = matcher.Match(fit.StartedAt, TimeSpan.FromSeconds(fit.ElapsedSeconds), plays);
        var context = await contextNamer.NameAsync(user.Id, songs, cancellationToken);
        var picks = highlights.Pick(songs, fit.ElapsedSeconds, fit.Streams);

        var run = new Run
        {
            ActivityId = ActivityIdOf(fit.StartedAt),
            Name = "Garmin run",
            SportType = "Run",
            StartedAt = fit.StartedAt,
            ElapsedSeconds = fit.ElapsedSeconds,
            Songs = RunRoute.WithPicks(songs, picks),
            ContextType = context.Type,
            ContextName = context.Name,
            Route = RunRoute.FromStreams(fit.Streams),
            StreamsRead = true, // the file is all there will ever be; nothing for the Strava backfill to fetch
        };
        await store.ReplaceAsync(user.Id, run, cancellationToken);
        logger.LogInformation("Stored uploaded run {StartedAt} with {SongCount} songs.", fit.StartedAt, run.Songs.Count);
        return run;
    }
}
