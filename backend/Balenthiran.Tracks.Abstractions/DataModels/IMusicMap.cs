namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A workout's route with the songs played during it pinned along it.</summary>
public interface IMusicMap
{
    string Id { get; set; }
    string Name { get; set; }
    DateTimeOffset StartedAt { get; set; }
    DateTimeOffset EndedAt { get; set; }
    bool HasRoute { get; set; }
    IReadOnlyList<IRoutePoint> Route { get; set; }
    IReadOnlyList<ISongPin> Songs { get; set; }
}
