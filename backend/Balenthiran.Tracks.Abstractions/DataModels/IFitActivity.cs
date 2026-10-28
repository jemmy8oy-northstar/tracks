namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A run read from a Garmin FIT file (BEH-SYNC-7): the same facts Tracks takes from a Strava activity and its streams.</summary>
public interface IFitActivity
{
    DateTimeOffset StartedAt { get; }

    int ElapsedSeconds { get; }

    /// <summary>The file's own samples, parallel like Strava's; <c>LatLng</c> is empty when the file has no GPS.</summary>
    IActivityStreams Streams { get; }
}
