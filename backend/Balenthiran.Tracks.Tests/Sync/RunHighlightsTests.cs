using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>
/// A 10 minute run sampled every second. Songs are placed by their offset into the run and length in
/// seconds; streams are built from a speed function so the distance is its running total.
/// </summary>
public class RunHighlightsTests
{
    private const int Elapsed = 600;

    private static readonly DateTimeOffset RunStart = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);
    private static readonly RunHighlights Highlights = new();

    private static IRunSong Song(int offset, int length) => new RunSong
    {
        Name = $"song@{offset}",
        PlayedFrom = RunStart.AddSeconds(offset),
        PlayedTo = RunStart.AddSeconds(offset + length),
        OffsetSeconds = offset,
    };

    private static ActivityStreams Streams(Func<int, double> speed, int seconds = Elapsed)
    {
        double[] time = [.. Enumerable.Range(0, seconds + 1).Select(s => (double)s)];
        double[] velocity = [.. time.Select(t => speed((int)t))];
        var distance = new double[time.Length];
        for (var i = 1; i < time.Length; i++)
            distance[i] = distance[i - 1] + velocity[i];
        return new ActivityStreams { TimeSeconds = time, DistanceMetres = distance, VelocityMetresPerSecond = velocity };
    }

    private static double Steady(int _) => 3;

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public void Top_speed_names_the_song_playing_at_the_fastest_moment()
    {
        IRunSong[] songs = [Song(0, 200), Song(200, 200), Song(400, 200)];
        var streams = Streams(t => t is >= 250 and <= 290 ? 5 : 3);

        Assert.Equal(1, Highlights.Pick(songs, Elapsed, streams).TopSpeedIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public void One_sample_GPS_spike_is_smoothed_away()
    {
        IRunSong[] songs = [Song(0, 300), Song(300, 300)];
        // A single 12 m/s jump in song 0 (raw, it beats everything), against a real, sustained 5 m/s
        // effort in song 1; averaged over five samples the jump is only 4.8.
        var streams = Streams(t => t == 100 ? 12 : t is >= 400 and <= 440 ? 5 : 3);

        Assert.Equal(1, Highlights.Pick(songs, Elapsed, streams).TopSpeedIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public void A_top_speed_in_a_gap_between_songs_names_no_song()
    {
        IRunSong[] songs = [Song(0, 100), Song(300, 300)];
        var streams = Streams(t => t is >= 150 and <= 200 ? 6 : 3);

        Assert.Null(Highlights.Pick(songs, Elapsed, streams).TopSpeedIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public void No_streams_name_nothing()
    {
        var picks = Highlights.Pick([Song(0, 300)], Elapsed, null);

        Assert.Null(picks.TopSpeedIndex);
        Assert.Null(picks.FastestPaceIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public void Without_a_velocity_stream_there_is_no_top_speed_but_pace_still_works()
    {
        var streams = Streams(Steady);
        streams.VelocityMetresPerSecond = [];

        var picks = Highlights.Pick([Song(0, 300), Song(300, 300)], Elapsed, streams);

        Assert.Null(picks.TopSpeedIndex);
        Assert.NotNull(picks.FastestPaceIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-3")]
    public void A_run_that_never_moved_names_no_top_speed()
    {
        Assert.Null(Highlights.Pick([Song(0, 600)], Elapsed, Streams(_ => 0)).TopSpeedIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-4")]
    public void Fastest_pace_names_the_song_that_covered_the_most_ground_per_second()
    {
        IRunSong[] songs = [Song(0, 200), Song(200, 200), Song(400, 200)];
        var streams = Streams(t => t < 200 ? 3 : t < 400 ? 3.5 : 3.2);

        Assert.Equal(1, Highlights.Pick(songs, Elapsed, streams).FastestPaceIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-4")]
    public void A_short_burst_inside_a_song_is_averaged_over_the_whole_song()
    {
        // Song 0 has a 10 s sprint at 8 m/s but averages 3.5; song 1 holds a steady 3.8.
        IRunSong[] songs = [Song(0, 300), Song(300, 300)];
        var streams = Streams(t => t is >= 100 and < 110 ? 8 : t < 300 ? 3 : 3.8);

        Assert.Equal(1, Highlights.Pick(songs, Elapsed, streams).FastestPaceIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-4")]
    public void A_song_covering_under_thirty_seconds_of_the_run_is_skipped()
    {
        // The last song has only its first 20 s inside the run, and they are the fastest of all.
        IRunSong[] songs = [Song(0, 580), Song(580, 240)];
        var streams = Streams(t => t >= 580 ? 9 : 3);

        Assert.Equal(0, Highlights.Pick(songs, Elapsed, streams).FastestPaceIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-4")]
    public void A_song_partly_outside_the_run_is_judged_only_on_the_part_inside()
    {
        // Song 0 started 100 s before the run and fills its first 100 s at 4 m/s; song 1 runs at 3.5.
        // Measured over its full 300 s length song 0 would look slower than it was inside the run.
        IRunSong[] songs = [Song(-100, 200), Song(100, 500)];
        var streams = Streams(t => t < 100 ? 4 : 3.5);

        Assert.Equal(0, Highlights.Pick(songs, Elapsed, streams).FastestPaceIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-4")]
    public void A_song_running_past_the_end_of_the_streams_is_clipped_to_them()
    {
        // The streams stop at 590 s; a song whose span says 500-700 is judged over 500-590 only.
        IRunSong[] songs = [Song(0, 500), Song(500, 200)];
        var streams = Streams(t => t < 500 ? 3 : 4, seconds: 590);

        Assert.Equal(1, Highlights.Pick(songs, Elapsed, streams).FastestPaceIndex);
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-4")]
    public void No_songs_or_a_stream_with_no_distance_name_nothing()
    {
        Assert.Null(Highlights.Pick([], Elapsed, Streams(Steady)).FastestPaceIndex);

        var streams = Streams(Steady);
        streams.DistanceMetres = [];
        var picks = Highlights.Pick([Song(0, 600)], Elapsed, streams);
        Assert.Null(picks.FastestPaceIndex);
        Assert.Null(picks.TopSpeedIndex);
    }
}
