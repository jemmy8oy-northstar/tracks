using Balenthiran.Tracks.Abstractions.DomainModels;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.DomainModels.Models;

public class DomainStatus : Status, IDomainStatus
{
    public string GetFriendlyStatus()
    {
        return $"System is running version {Version} (Updated: {LastUpdated:g})";
    }
}
