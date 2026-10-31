using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class StravaActivity : IStravaActivity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SportType { get; set; } = string.Empty;
    public DateTimeOffset StartDate { get; set; }
    public int ElapsedTimeSeconds { get; set; }
    public string? Description { get; set; }
}
