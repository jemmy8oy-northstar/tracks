using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests;

public class MusicMapServiceTests
{
    private readonly MusicMapService service =
        new(new DemoWorkoutSource(), new SongMatcher(new HaversineDistanceCalculator()));

    [Fact]
    public async Task Asking_for_an_unknown_workout_throws_NotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetMusicMapAsync("missing"));
    }

    [Fact]
    public async Task The_workout_list_is_oldest_first_and_flags_which_have_a_route()
    {
        var list = await service.ListWorkoutsAsync();

        Assert.Equal(["park-run", "indoor-ride"], list.Select(w => w.Id));
        Assert.Equal([true, false], list.Select(w => w.HasRoute));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-1")]
    public async Task BEH_MAP_1_the_demo_run_matches_the_songs_that_played_during_it_and_no_others()
    {
        var map = await service.GetMusicMapAsync("park-run");

        Assert.Equal(8, map.Songs.Count);
        Assert.Equal("Tempo Lines", map.Songs[0].Title);
        Assert.Equal("Home Straight", map.Songs[^1].Title);
    }
}
