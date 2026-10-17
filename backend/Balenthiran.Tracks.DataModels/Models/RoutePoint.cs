using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class RoutePoint : IRoutePoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset Time { get; set; }
}
