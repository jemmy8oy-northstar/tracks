using Balenthiran.Tracks.Database;
using Balenthiran.Tracks.EntityModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>A fresh in-memory database for service-level tests, reachable as contexts or as a scope factory.</summary>
public sealed class TestDb : IDisposable
{
    private readonly ServiceProvider services;
    private readonly DbContextOptions<AppDbContext> options;

    public TestDb()
    {
        var name = Guid.NewGuid().ToString();
        options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options;
        services = new ServiceCollection().AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(name)).BuildServiceProvider();
    }

    public IServiceScopeFactory Scopes => services.GetRequiredService<IServiceScopeFactory>();

    /// <summary>A new context over the same store; use a new one to read back what another wrote.</summary>
    public AppDbContext Context() => new(options);

    public async Task<UserEntity> AddUserAsync(
        long athleteId, string stravaRefresh = "strava-0", string? spotifyRefresh = "spotify-0", string? stravaScope = null, bool writeSongs = false)
    {
        using var db = Context();
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            StravaAthleteId = athleteId,
            StravaRefreshToken = stravaRefresh,
            SpotifyRefreshToken = spotifyRefresh,
            StravaScope = stravaScope,
            WriteSongsToStravaDescription = writeSongs,
            CreatedAt = DateTimeOffset.UnixEpoch,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<UserEntity> ReloadAsync(Guid userId)
    {
        using var db = Context();
        return await db.Users.SingleAsync(u => u.Id == userId);
    }

    public void Dispose() => services.Dispose();
}
