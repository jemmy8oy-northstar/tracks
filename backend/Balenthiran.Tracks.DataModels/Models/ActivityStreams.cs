using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class ActivityStreams : IActivityStreams
{
    public IReadOnlyList<double> TimeSeconds { get; set; } = [];
    public IReadOnlyList<double> DistanceMetres { get; set; } = [];
    public IReadOnlyList<double> VelocityMetresPerSecond { get; set; } = [];
}
