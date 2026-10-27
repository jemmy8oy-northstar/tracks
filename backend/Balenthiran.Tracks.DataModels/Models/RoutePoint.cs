using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public sealed record RoutePoint(double Lat, double Lng, double Seconds) : IRoutePoint;
