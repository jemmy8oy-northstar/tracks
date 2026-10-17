using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class SongPin : ISongPin
{
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required string CoverColour { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int ElapsedSeconds { get; set; }
    public double? DistanceMetres { get; set; }
    public IRoutePoint? Pin { get; set; }
}
