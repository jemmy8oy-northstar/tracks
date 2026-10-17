using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IDistanceCalculator
{
    /// <summary>Great-circle distance in metres between two route points.</summary>
    double BetweenMetres(IRoutePoint from, IRoutePoint to);
}
