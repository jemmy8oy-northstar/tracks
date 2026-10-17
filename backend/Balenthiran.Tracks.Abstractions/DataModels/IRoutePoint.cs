namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>One GPS fix on a workout's route.</summary>
public interface IRoutePoint
{
    double Latitude { get; set; }
    double Longitude { get; set; }
    DateTimeOffset Time { get; set; }
}
