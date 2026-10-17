namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A signed-up person. Deliberately carries no tokens — those never leave the services.</summary>
public interface IUser
{
    Guid Id { get; }
    long StravaAthleteId { get; }
    bool SpotifyConnected { get; }
}
