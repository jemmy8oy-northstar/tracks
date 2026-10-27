namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>One point of a run's route (BEH-MAP-2): where the runner was, and how far into the run.</summary>
public interface IRoutePoint
{
    double Lat { get; }
    double Lng { get; }

    /// <summary>Seconds from the run's start.</summary>
    double Seconds { get; }
}
