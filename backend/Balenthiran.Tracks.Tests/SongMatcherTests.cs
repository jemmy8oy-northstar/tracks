using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using static Balenthiran.Tracks.Tests.MatcherFixtures;

namespace Balenthiran.Tracks.Tests;

/// <summary>
/// BEH-MAP-2 (matching), BEH-MAP-1 (distance per song) and BEH-MAP-3 (no GPS) at the service level.
/// </summary>
public class SongMatcherTests
{
    private readonly SongMatcher matcher = new(new HaversineDistanceCalculator());

    // ---- BEH-MAP-2: which songs belong to a workout ----------------------------------------

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_a_song_that_started_inside_the_workout_window_belongs_to_it()
    {
        var map = matcher.Match(Run(), [Play("inside", 25)]);

        Assert.Equal(["inside"], map.Songs.Select(s => s.Title));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_songs_that_started_before_or_after_the_window_are_left_out()
    {
        var map = matcher.Match(Run(), [Play("before", -1), Play("inside", 30), Play("after", 61)]);

        Assert.Equal(["inside"], map.Songs.Select(s => s.Title));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_a_song_starting_exactly_at_the_workout_start_or_end_belongs_to_it()
    {
        var map = matcher.Match(Run(), [Play("at-start", 0), Play("at-end", 60)]);

        Assert.Equal(["at-start", "at-end"], map.Songs.Select(s => s.Title));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_songs_are_ordered_by_when_they_started_whatever_order_the_history_arrives_in()
    {
        var map = matcher.Match(Run(), [Play("third", 40), Play("first", 5), Play("second", 20)]);

        Assert.Equal(["first", "second", "third"], map.Songs.Select(s => s.Title));
    }

    // ---- BEH-MAP-2: where the pin goes ------------------------------------------------------

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_the_pin_is_the_route_point_whose_timestamp_is_nearest_the_song_start()
    {
        var map = matcher.Match(Run(), [Play("near-third-fix", 22)]);

        // Fixes are at 0,10,20,30...; 22s is nearest the 20s fix, the third (longitude 0.002).
        var pin = Assert.Single(map.Songs).Pin;
        Assert.NotNull(pin);
        Assert.Equal(Start.AddSeconds(20), pin!.Time);
        Assert.Equal(0.002, pin.Longitude, 6);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_a_song_that_starts_halfway_between_two_fixes_takes_the_earlier_one()
    {
        var map = matcher.Match(Run(), [Play("halfway", 25)]);

        Assert.Equal(Start.AddSeconds(20), Assert.Single(map.Songs).Pin!.Time);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_route_points_given_out_of_order_still_match_by_timestamp()
    {
        var workout = Run();
        workout.Route = workout.Route.Reverse().ToList();

        var map = matcher.Match(workout, [Play("a", 41)]);

        Assert.Equal(Start.AddSeconds(40), Assert.Single(map.Songs).Pin!.Time);
        Assert.Equal(workout.Route.Count, map.Route.Count);
        Assert.True(map.Route.Zip(map.Route.Skip(1), (a, b) => a.Time <= b.Time).All(ok => ok));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_a_song_that_starts_after_the_last_fix_is_pinned_to_the_last_fix()
    {
        // GPS stopped recording at 60s but the workout (and the song) run on to 90s.
        var map = matcher.Match(Run(seconds: 90), [Play("late", 85)]);

        Assert.Equal(Start.AddSeconds(60), Assert.Single(map.Songs).Pin!.Time);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_a_workout_with_no_songs_in_its_window_still_shows_its_route_with_no_pins()
    {
        var map = matcher.Match(Run(), [Play("yesterday", -86_400), Play("later", 3_600)]);

        Assert.True(map.HasRoute);
        Assert.Equal(7, map.Route.Count);
        Assert.Empty(map.Songs);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_a_workout_with_an_empty_listening_history_still_shows_its_route()
    {
        var map = matcher.Match(Run(), []);

        Assert.True(map.HasRoute);
        Assert.Equal(7, map.Route.Count);
        Assert.Empty(map.Songs);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void BEH_MAP_2_the_song_keeps_its_title_artist_and_cover()
    {
        var map = matcher.Match(Run(), [Play("Title", 10)]);

        var song = Assert.Single(map.Songs);
        Assert.Equal("Title", song.Title);
        Assert.Equal("Test Artist", song.Artist);
        Assert.Equal("#112233", song.CoverColour);
    }

    // ---- BEH-MAP-1: the list shows the distance each song covered ---------------------------

    [Fact]
    [Trait("Behaviour", "BEH-MAP-1")]
    public void BEH_MAP_1_each_song_covers_the_distance_until_the_next_song_starts()
    {
        var map = matcher.Match(Run(), [Play("a", 0), Play("b", 30)]);

        // a: fix 0 -> fix 3 (3 steps); b: fix 3 -> workout end at 60s = fix 6 (3 steps).
        Assert.Equal(3 * MetresPerStep, map.Songs[0].DistanceMetres!.Value, 0);
        Assert.Equal(3 * MetresPerStep, map.Songs[1].DistanceMetres!.Value, 0);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-1")]
    public void BEH_MAP_1_a_song_shorter_than_the_gap_covers_only_the_ground_while_it_played()
    {
        var map = matcher.Match(Run(), [Play("short", 0, durationSeconds: 20), Play("next", 50)]);

        Assert.Equal(2 * MetresPerStep, map.Songs[0].DistanceMetres!.Value, 0);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-1")]
    public void BEH_MAP_1_the_last_song_stops_counting_when_the_workout_ends()
    {
        var map = matcher.Match(Run(), [Play("last", 40, durationSeconds: 3_600)]);

        Assert.Equal(2 * MetresPerStep, map.Songs[0].DistanceMetres!.Value, 0);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-1")]
    public void BEH_MAP_1_the_list_gives_each_song_its_elapsed_time_from_the_workout_start()
    {
        var map = matcher.Match(Run(), [Play("a", 0), Play("b", 37)]);

        Assert.Equal([0, 37], map.Songs.Select(s => s.ElapsedSeconds));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-1")]
    public void BEH_MAP_1_overlapping_plays_never_give_a_negative_distance()
    {
        var map = matcher.Match(Run(), [Play("a", 30), Play("b", 30)]);

        Assert.All(map.Songs, s => Assert.True(s.DistanceMetres >= 0));
    }

    // ---- BEH-MAP-3: a workout recorded without GPS ----------------------------------------

    [Fact]
    [Trait("Behaviour", "BEH-MAP-3")]
    public void BEH_MAP_3_a_workout_without_gps_lists_its_songs_by_elapsed_time()
    {
        var map = matcher.Match(Indoor(), [Play("a", 0), Play("b", 45), Play("outside", 500)]);

        Assert.Equal(["a", "b"], map.Songs.Select(s => s.Title));
        Assert.Equal([0, 45], map.Songs.Select(s => s.ElapsedSeconds));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-3")]
    public void BEH_MAP_3_a_workout_without_gps_draws_no_map_and_pins_nothing()
    {
        var map = matcher.Match(Indoor(), [Play("a", 10)]);

        Assert.False(map.HasRoute);
        Assert.Empty(map.Route);
        var song = Assert.Single(map.Songs);
        Assert.Null(song.Pin);
        Assert.Null(song.DistanceMetres);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-3")]
    public void BEH_MAP_3_a_workout_without_gps_or_songs_is_just_empty()
    {
        IMusicMap map = matcher.Match(Indoor(), []);

        Assert.False(map.HasRoute);
        Assert.Empty(map.Songs);
    }
}
