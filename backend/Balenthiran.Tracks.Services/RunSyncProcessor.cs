using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

public class RunSyncProcessor(
    IStravaSettings stravaSettings,
    ISpotifySettings spotifySettings,
    IUserService users,
    IStravaClient strava,
    ISpotifyClient spotify,
    IRunSongMatcher matcher,
    IRunHighlights highlights,
    IRunStore store,
    ILogger<RunSyncProcessor> logger) : IRunSyncProcessor
{
    public async Task ProcessAsync(long athleteId, long activityId, CancellationToken cancellationToken = default)
    {
        if (!stravaSettings.IsConfigured || !spotifySettings.IsConfigured)
        {
            logger.LogWarning("Skipping activity {ActivityId}: Strava or Spotify app credentials are not configured.", activityId);
            return;
        }

        var user = await users.FindByAthleteAsync(athleteId, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("Skipping activity {ActivityId}: its athlete is not a user.", activityId);
            return;
        }

        // Recently-played only keeps the last 50 plays and cannot be read back later, so with no
        // Spotify there is nothing to pair the run with.
        if (!user.SpotifyConnected)
        {
            logger.LogInformation("Skipping activity {ActivityId}: the user has not connected Spotify.", activityId);
            return;
        }

        var activity = await strava.GetActivityAsync(user.Id, activityId, cancellationToken);
        if (!stravaSettings.RunSportTypes.Contains(activity.SportType, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogInformation("Ignoring activity {ActivityId}: {SportType} is not a run.", activityId, activity.SportType);
            return;
        }

        var plays = await spotify.GetRecentlyPlayedAsync(user.Id, activity.StartDate, cancellationToken);
        var songs = matcher.Match(activity.StartDate, TimeSpan.FromSeconds(activity.ElapsedTimeSeconds), plays);
        var context = await ContextOfAsync(user.Id, activity.Id, songs, cancellationToken);
        var (withPicks, route, streamsRead) = await WithStreamsAsync(user.Id, activity.Id, activity.ElapsedTimeSeconds, songs, cancellationToken);
        songs = withPicks;

        await store.SaveAsync(user.Id, new Run
        {
            ActivityId = activity.Id,
            Name = activity.Name,
            SportType = activity.SportType,
            StartedAt = activity.StartDate,
            ElapsedSeconds = activity.ElapsedTimeSeconds,
            Songs = songs,
            ContextType = context.Type,
            ContextName = context.Name,
            Route = route,
            StreamsRead = streamsRead,
        }, cancellationToken);
        logger.LogInformation("Stored run {ActivityId} with {SongCount} songs.", activityId, songs.Count);
    }

    // The context is a bonus too: a name Spotify will not give (a private or editorial playlist, a
    // rate limit, no network) leaves the type without a name and never costs the run.
    private async Task<(string? Type, string? Name)> ContextOfAsync(
        Guid userId, long activityId, IReadOnlyList<IRunSong> songs, CancellationToken cancellationToken)
    {
        var pick = RunContextMajority.Pick(songs);
        if (pick.Uri is null)
            return (pick.Type, null);

        try
        {
            return (pick.Type, await spotify.GetContextNameAsync(userId, pick.Type!, pick.Uri, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Storing activity {ActivityId} with its {ContextType} unnamed: the name could not be read.", activityId, pick.Type);
            return (pick.Type, null);
        }
    }

    // Highlights and the route are a bonus: the streams being unavailable (rate limit, Strava down)
    // must never cost the run itself, which can only be paired with Spotify's recent plays right now.
    // Unread streams leave StreamsRead false, so the backfill tries again later (BEH-SYNC-6).
    private async Task<(IReadOnlyList<IRunSong> Songs, IReadOnlyList<IRoutePoint> Route, bool StreamsRead)> WithStreamsAsync(
        Guid userId, long activityId, int elapsedSeconds, IReadOnlyList<IRunSong> songs, CancellationToken cancellationToken)
    {
        try
        {
            var streams = await strava.GetStreamsAsync(userId, activityId, cancellationToken);
            return (RunRoute.WithPicks(songs, highlights.Pick(songs, elapsedSeconds, streams)), RunRoute.FromStreams(streams), true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Storing activity {ActivityId} without highlights or route: its streams could not be read.", activityId);
            return (songs, [], false);
        }
    }
}
