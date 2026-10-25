using Balenthiran.Tracks.EntityModels;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    // Data Protection keys (cookie signing/encryption) live here so a redeploy doesn't sign everyone out.
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<RunEntity> Runs => Set<RunEntity>();

    public DbSet<RunSongEntity> RunSongs => Set<RunSongEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // One account per Strava athlete; signing in again finds the same row.
        modelBuilder.Entity<UserEntity>(e => e.HasIndex(x => x.StravaAthleteId).IsUnique());

        // A user's runs are listed newest first; a Strava activity is stored once per user.
        // Deleting a user (Strava deauthorisation) deletes their runs and songs with them.
        modelBuilder.Entity<RunEntity>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.ActivityId }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.StartedAt });
            e.HasOne<UserEntity>().WithMany(u => u.Runs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RunSongEntity>(e =>
        {
            e.HasIndex(x => new { x.RunId, x.Position });
            e.HasOne<RunEntity>().WithMany(r => r.Songs).HasForeignKey(x => x.RunId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
