using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>
/// played_at is when a play ended, so a play spans [played_at - duration, played_at]. A 4 minute
/// song is used throughout against a run from 10:00 to 10:30.
/// </summary>
public class RunSongMatcherTests
{
    private static readonly DateTimeOffset RunStart = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Elapsed = TimeSpan.FromMinutes(30);
    private static readonly RunSongMatcher Matcher = new();

    private static SpotifyPlay Play(string name, DateTimeOffset endedAt, int minutes = 4) => new()
    {
        TrackId = name,
        TrackName = name,
        Artists = ["Artist"],
        DurationMs = (int)TimeSpan.FromMinutes(minutes).TotalMilliseconds,
        PlayedAt = endedAt,
    };

    [Fact]
    public void A_song_inside_the_run_is_kept_with_its_offset()
    {
        var songs = Matcher.Match(RunStart, Elapsed, [Play("inside", RunStart.AddMinutes(14))]);

        var song = Assert.Single(songs);
        Assert.Equal("inside", song.Name);
        Assert.Equal(RunStart.AddMinutes(10), song.PlayedFrom);
        Assert.Equal(RunStart.AddMinutes(14), song.PlayedTo);
        Assert.Equal(600, song.OffsetSeconds);
    }

    [Fact]
    public void A_song_ending_exactly_at_the_start_is_dropped()
    {
        Assert.Empty(Matcher.Match(RunStart, Elapsed, [Play("before", RunStart)]));
    }

    [Fact]
    public void A_song_spanning_the_start_is_kept_with_a_negative_offset()
    {
        var songs = Matcher.Match(RunStart, Elapsed, [Play("spans start", RunStart.AddMinutes(1))]);

        var song = Assert.Single(songs);
        Assert.Equal(-180, song.OffsetSeconds);
    }

    [Fact]
    public void A_song_spanning_the_end_is_kept()
    {
        var songs = Matcher.Match(RunStart, Elapsed, [Play("spans end", RunStart.AddMinutes(32))]);

        Assert.Single(songs);
    }

    [Fact]
    public void A_song_starting_exactly_at_the_end_is_dropped()
    {
        // 4 minutes long, ending at 10:34, so it starts at 10:30 - the instant the run finishes.
        Assert.Empty(Matcher.Match(RunStart, Elapsed, [Play("touches end", RunStart.AddMinutes(34))]));
    }

    [Fact]
    public void A_song_fully_after_the_run_is_dropped()
    {
        Assert.Empty(Matcher.Match(RunStart, Elapsed, [Play("after", RunStart.AddMinutes(60))]));
    }

    [Fact]
    public void A_song_longer_than_the_run_that_covers_it_is_kept()
    {
        var songs = Matcher.Match(RunStart, TimeSpan.FromMinutes(2), [Play("long", RunStart.AddMinutes(5), minutes: 10)]);

        Assert.Single(songs);
    }

    [Fact]
    public void Songs_come_back_in_the_order_they_played_whatever_order_they_arrive_in()
    {
        // Spotify lists newest first.
        var songs = Matcher.Match(RunStart, Elapsed,
            [Play("third", RunStart.AddMinutes(26)), Play("second", RunStart.AddMinutes(18)), Play("first", RunStart.AddMinutes(8))]);

        Assert.Equal(["first", "second", "third"], songs.Select(s => s.Name));
    }

    [Fact]
    public void No_plays_gives_an_empty_list_not_an_error()
    {
        Assert.Empty(Matcher.Match(RunStart, Elapsed, []));
    }
}
