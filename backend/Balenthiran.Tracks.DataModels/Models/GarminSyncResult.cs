using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class GarminSyncResult : IGarminSyncResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
}
