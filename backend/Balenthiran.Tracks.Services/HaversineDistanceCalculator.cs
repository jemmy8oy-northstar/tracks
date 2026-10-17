using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.Services;

public class HaversineDistanceCalculator : IDistanceCalculator
{
    private const double EarthRadiusMetres = 6_371_000;

    public double BetweenMetres(IRoutePoint from, IRoutePoint to)
    {
        var lat1 = ToRadians(from.Latitude);
        var lat2 = ToRadians(to.Latitude);
        var dLat = lat2 - lat1;
        var dLon = ToRadians(to.Longitude - from.Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * EarthRadiusMetres * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
