using Balenthiran.Tracks.Database;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Tests;

public class MigrationTests
{
    // Never connects: the check compares the model with the last migration's snapshot.
    private static AppDbContext Postgres() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=unused", b => b.MigrationsAssembly("Balenthiran.Tracks.Database"))
        .Options);

    [Fact]
    public void The_migrations_describe_the_current_model()
    {
        using var db = Postgres();

        Assert.False(db.Database.HasPendingModelChanges(), "Run `dotnet ef migrations add` — the model has changed since the last migration.");
    }

    [Fact]
    public void The_model_stores_data_protection_keys()
    {
        using var db = Postgres();

        Assert.NotNull(db.Model.FindEntityType(typeof(Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey)));
    }

    [Fact]
    public void There_is_a_migration_to_apply()
    {
        using var db = Postgres();

        Assert.NotEmpty(db.Database.GetMigrations());
    }
}
