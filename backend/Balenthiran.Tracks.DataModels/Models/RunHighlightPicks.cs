using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class RunHighlightPicks : IRunHighlightPicks
{
    public int? TopSpeedIndex { get; set; }
    public int? FastestPaceIndex { get; set; }
}
