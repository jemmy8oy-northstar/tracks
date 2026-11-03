using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class GarminRun : IGarminRun
{
    public long ActivityId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
}
