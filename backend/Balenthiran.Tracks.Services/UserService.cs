using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Database;
using Balenthiran.Tracks.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Services;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<IUser> GetSignedInAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("This account no longer exists.");
        return ToUser(user);
    }

    public async Task<IUser?> FindByAthleteAsync(long athleteId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.StravaAthleteId == athleteId, cancellationToken);
        return user is null ? null : ToUser(user);
    }

    public async Task SetWriteSongsToStravaAsync(Guid userId, bool enabled, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("This account no longer exists.");
        if (enabled && !HasWriteScope(user))
            throw new ConflictException("Tracks needs permission to edit your Strava activities. Reconnect Strava to turn this on.");

        user.WriteSongsToStravaDescription = enabled;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByAthleteAsync(long athleteId, CancellationToken cancellationToken = default)
    {
        // Load the runs and songs so the delete cascades on every provider, not just the relational ones.
        var user = await db.Users
            .Include(u => u.Runs).ThenInclude(r => r.Songs)
            .FirstOrDefaultAsync(u => u.StravaAthleteId == athleteId, cancellationToken);
        if (user is null)
            return;

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool HasWriteScope(UserEntity user) =>
        user.StravaScope is not null && user.StravaScope.Split(',').Contains(StravaAuthService.WriteScope);

    private static User ToUser(UserEntity user) => new()
    {
        Id = user.Id,
        StravaAthleteId = user.StravaAthleteId,
        SpotifyConnected = user.SpotifyRefreshToken is not null,
        StravaWriteGranted = HasWriteScope(user),
        WriteSongsToStravaDescription = user.WriteSongsToStravaDescription,
    };
}
