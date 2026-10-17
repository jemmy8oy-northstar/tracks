using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

public class RunSyncProcessor(
    IStravaSettings stravaSettings,
    ISpotifySettings spotifySettings,
    IStravaClient strava,
    ISpotifyClient spotify,
    IRunSongMatcher matcher,
    IRunStore store,
    ILogger<RunSyncProcessor> logger) : IRunSyncProcessor
{
    public async Task ProcessAsync(long activityId, CancellationToken cancellationToken = default)
    {
        if (!stravaSettings.IsConfigured || !spotifySettings.IsConfigured)
        {
            logger.LogWarning("Skipping activity {ActivityId}: Strava or Spotify credentials are not configured.", activityId);
            return;
        }

        var activity = await strava.GetActivityAsync(activityId, cancellationToken);
        if (!stravaSettings.RunSportTypes.Contains(activity.SportType, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogInformation("Ignoring activity {ActivityId}: {SportType} is not a run.", activityId, activity.SportType);
            return;
        }

        // Recently-played only keeps the last 50 plays, so read it now rather than later.
        var plays = await spotify.GetRecentlyPlayedAsync(activity.StartDate, cancellationToken);
        var songs = matcher.Match(activity.StartDate, TimeSpan.FromSeconds(activity.ElapsedTimeSeconds), plays);

        store.Save(new Run
        {
            ActivityId = activity.Id,
            Name = activity.Name,
            SportType = activity.SportType,
            StartedAt = activity.StartDate,
            ElapsedSeconds = activity.ElapsedTimeSeconds,
            Songs = songs,
        });
        logger.LogInformation("Stored run {ActivityId} with {SongCount} songs.", activityId, songs.Count);
    }
}
