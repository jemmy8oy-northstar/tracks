using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IUserService
{
    /// <summary>The user behind a session. Throws <c>UnauthorizedException</c> if the account no longer exists.</summary>
    Task<IUser> GetSignedInAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>The user with that Strava athlete id, or null — webhooks routinely name athletes we do not know.</summary>
    Task<IUser?> FindByAthleteAsync(long athleteId, CancellationToken cancellationToken = default);

    /// <summary>Deletes the user and all their runs. Does nothing for an unknown athlete.</summary>
    Task DeleteByAthleteAsync(long athleteId, CancellationToken cancellationToken = default);
}
