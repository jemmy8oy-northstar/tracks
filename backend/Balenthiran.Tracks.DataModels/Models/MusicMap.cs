using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class MusicMap : IMusicMap
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public bool HasRoute { get; set; }
    public IReadOnlyList<IRoutePoint> Route { get; set; } = [];
    public IReadOnlyList<ISongPin> Songs { get; set; } = [];
}
