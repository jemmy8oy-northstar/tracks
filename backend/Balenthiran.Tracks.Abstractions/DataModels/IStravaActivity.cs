namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>The parts of a Strava activity Tracks needs.</summary>
public interface IStravaActivity
{
    long Id { get; }
    string Name { get; }
    string SportType { get; }
    DateTimeOffset StartDate { get; }
    int ElapsedTimeSeconds { get; }
}
