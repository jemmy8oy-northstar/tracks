namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A recorded workout. <see cref="Route"/> is empty when it was recorded without GPS.</summary>
public interface IWorkout
{
    string Id { get; set; }
    string Name { get; set; }
    DateTimeOffset StartedAt { get; set; }
    DateTimeOffset EndedAt { get; set; }
    IReadOnlyList<IRoutePoint> Route { get; set; }
}
