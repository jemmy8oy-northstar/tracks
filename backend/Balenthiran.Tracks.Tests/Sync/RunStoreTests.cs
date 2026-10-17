using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunStoreTests
{
    private static Run RunAt(long id, int hour, string name = "run") => new()
    {
        ActivityId = id,
        Name = name,
        StartedAt = new DateTimeOffset(2026, 10, 17, hour, 0, 0, TimeSpan.Zero),
    };

    private static RunSong Song(string name, int offset) => new()
    {
        Name = name,
        Artists = ["A", "B"],
        AlbumArtUrl = "https://img/1.jpg",
        PlayedFrom = new DateTimeOffset(2026, 10, 17, 9, 0, 0, TimeSpan.Zero).AddSeconds(offset),
        PlayedTo = new DateTimeOffset(2026, 10, 17, 9, 4, 0, TimeSpan.Zero).AddSeconds(offset),
        OffsetSeconds = offset,
    };

    [Fact]
    public async Task Runs_come_back_newest_first_whatever_order_they_were_saved_in()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var store = new RunStore(db.Context());
        await store.SaveAsync(user.Id, RunAt(1, 8));
        await store.SaveAsync(user.Id, RunAt(3, 18));
        await store.SaveAsync(user.Id, RunAt(2, 12));

        Assert.Equal([3L, 2L, 1L], (await new RunStore(db.Context()).GetAllAsync(user.Id)).Select(r => r.ActivityId));
    }

    [Fact]
    public async Task Saving_the_same_activity_twice_keeps_the_first()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var store = new RunStore(db.Context());

        Assert.True(await store.SaveAsync(user.Id, RunAt(1, 8, "original")));
        Assert.False(await new RunStore(db.Context()).SaveAsync(user.Id, RunAt(1, 8, "duplicate")));

        Assert.Equal("original", Assert.Single(await new RunStore(db.Context()).GetAllAsync(user.Id)).Name);
    }

    [Fact]
    public async Task Songs_round_trip_in_the_order_they_were_given()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var run = RunAt(1, 9);
        run.SportType = "Run";
        run.ElapsedSeconds = 1800;
        run.Songs = [Song("second alphabetically", 0), Song("a first", 300), Song("middle", -20)];
        await new RunStore(db.Context()).SaveAsync(user.Id, run);

        var stored = Assert.Single(await new RunStore(db.Context()).GetAllAsync(user.Id));

        Assert.Equal("Run", stored.SportType);
        Assert.Equal(1800, stored.ElapsedSeconds);
        Assert.Equal(["second alphabetically", "a first", "middle"], stored.Songs.Select(s => s.Name));
        var first = stored.Songs[0];
        Assert.Equal(["A", "B"], first.Artists);
        Assert.Equal("https://img/1.jpg", first.AlbumArtUrl);
        Assert.Equal(run.Songs[0].PlayedTo, first.PlayedTo);
        Assert.Equal(-20, stored.Songs[2].OffsetSeconds);
    }

    [Fact]
    public async Task Two_users_can_each_have_the_same_activity_and_only_see_their_own_runs()
    {
        using var db = new TestDb();
        var ann = await db.AddUserAsync(1);
        var bob = await db.AddUserAsync(2);

        Assert.True(await new RunStore(db.Context()).SaveAsync(ann.Id, RunAt(1, 8, "ann's")));
        Assert.True(await new RunStore(db.Context()).SaveAsync(bob.Id, RunAt(1, 8, "bob's")));

        Assert.Equal("ann's", Assert.Single(await new RunStore(db.Context()).GetAllAsync(ann.Id)).Name);
        Assert.Equal("bob's", Assert.Single(await new RunStore(db.Context()).GetAllAsync(bob.Id)).Name);
    }

    [Fact]
    public async Task A_user_with_no_runs_gets_an_empty_list()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);

        Assert.Empty(await new RunStore(db.Context()).GetAllAsync(user.Id));
        Assert.Equal(0, await db.Context().Runs.CountAsync());
    }
}
