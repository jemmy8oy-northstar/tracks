using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunStoreTests
{
    private static Run RunAt(long id, int hour, string name = "run") => new()
    {
        ActivityId = id,
        Name = name,
        StartedAt = new DateTimeOffset(2026, 10, 17, hour, 0, 0, TimeSpan.Zero),
    };

    [Fact]
    public void Runs_come_back_newest_first_whatever_order_they_were_saved_in()
    {
        var store = new RunStore();
        store.Save(RunAt(1, 8));
        store.Save(RunAt(3, 18));
        store.Save(RunAt(2, 12));

        Assert.Equal([3L, 2L, 1L], store.GetAll().Select(r => r.ActivityId));
    }

    [Fact]
    public void Saving_the_same_activity_twice_keeps_the_first()
    {
        var store = new RunStore();

        Assert.True(store.Save(RunAt(1, 8, "original")));
        Assert.False(store.Save(RunAt(1, 8, "duplicate")));

        Assert.Equal("original", Assert.Single(store.GetAll()).Name);
    }

    [Fact]
    public async Task Concurrent_saves_all_land()
    {
        var store = new RunStore();

        await Task.WhenAll(Enumerable.Range(1, 200).Select(i => Task.Run(() => store.Save(RunAt(i, i % 24)))));

        Assert.Equal(200, store.GetAll().Count);
    }
}
