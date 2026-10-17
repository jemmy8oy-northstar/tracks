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

        await store.SaveAsync(user.Id, new Run
        {
            ActivityId = activity.Id,
            Name = activity.Name,
            SportType = activity.SportType,
            StartedAt = activity.StartDate,
            ElapsedSeconds = activity.ElapsedTimeSeconds,
            Songs = songs,
        }, cancellationToken);
        logger.LogInformation("Stored run {ActivityId} with {SongCount} songs.", activityId, songs.Count);
    }
}
