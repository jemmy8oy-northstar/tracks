namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>The second-by-second GPS streams of a Strava activity; the lists are parallel (same sample, same index).</summary>
public interface IActivityStreams
{
    /// <summary>Seconds from the activity's start.</summary>
    IReadOnlyList<double> TimeSeconds { get; }

    /// <summary>Cumulative distance in metres.</summary>
    IReadOnlyList<double> DistanceMetres { get; }

    /// <summary>Strava's smoothed speed in metres per second; empty when Strava sent none.</summary>
    IReadOnlyList<double> VelocityMetresPerSecond { get; }
}
