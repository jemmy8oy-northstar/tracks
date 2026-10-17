namespace Balenthiran.Tracks.Abstractions.DomainModels;

using Balenthiran.Tracks.Abstractions.DataModels;

public interface IDomainStatus : IStatus
{
    string GetFriendlyStatus();
}
