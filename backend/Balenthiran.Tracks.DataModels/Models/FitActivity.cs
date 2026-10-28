using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class FitActivity : IFitActivity
{
    public DateTimeOffset StartedAt { get; set; }
    public int ElapsedSeconds { get; set; }
    public IActivityStreams Streams { get; set; } = new ActivityStreams();
}
