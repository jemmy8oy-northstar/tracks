namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A signed-up person. Deliberately carries no tokens — those never leave the services.</summary>
public interface IUser
{
    Guid Id { get; }
    long StravaAthleteId { get; }
    bool SpotifyConnected { get; }

    /// <summary>Strava granted <c>activity:write</c> at the last sign-in. False for a runner who has not reconnected since Tracks began asking.</summary>
    bool StravaWriteGranted { get; }

    /// <summary>BEH-SHARE-2: the runner turned on writing the songs into their Strava activity description.</summary>
    bool WriteSongsToStravaDescription { get; }
}
