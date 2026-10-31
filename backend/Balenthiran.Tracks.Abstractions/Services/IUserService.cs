using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IUserService
{
    /// <summary>The user behind a session. Throws <c>UnauthorizedException</c> if the account no longer exists.</summary>
    Task<IUser> GetSignedInAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>The user with that Strava athlete id, or null — webhooks routinely name athletes we do not know.</summary>
    Task<IUser?> FindByAthleteAsync(long athleteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// BEH-SHARE-2: turns writing songs into Strava descriptions on or off. Turning it on needs the
    /// <c>activity:write</c> grant; without it this throws <c>ConflictException</c> (the runner must reconnect Strava).
    /// </summary>
    Task SetWriteSongsToStravaAsync(Guid userId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Deletes the user and all their runs. Does nothing for an unknown athlete.</summary>
    Task DeleteByAthleteAsync(long athleteId, CancellationToken cancellationToken = default);
}
