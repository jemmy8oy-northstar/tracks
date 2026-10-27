using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class Run : IRun
{
    public long ActivityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SportType { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public int ElapsedSeconds { get; set; }
    public IReadOnlyList<IRunSong> Songs { get; set; } = [];
    public string? ContextType { get; set; }
    public string? ContextName { get; set; }
    public IReadOnlyList<IRoutePoint> Route { get; set; } = [];
    public bool StreamsRead { get; set; }
}
