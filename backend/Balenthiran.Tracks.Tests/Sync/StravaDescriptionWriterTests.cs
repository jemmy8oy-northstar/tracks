using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class StravaDescriptionWriterTests
{
    private const string Start = StravaDescriptionWriter.BlockStart;
    private const string End = StravaDescriptionWriter.BlockEnd;

    private sealed class RecordingStrava(bool fails = false) : IStravaClient
    {
        public List<(long ActivityId, string Description)> Updates { get; } = [];

        public Task UpdateDescriptionAsync(Guid userId, long activityId, string description, CancellationToken cancellationToken = default)
        {
            Updates.Add((activityId, description));
            return fails ? throw new UpstreamServiceException("Strava description update failed with status 403.") : Task.CompletedTask;
        }

        public Task<IStravaActivity> GetActivityAsync(Guid userId, long activityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IActivityStreams?> GetStreamsAsync(Guid userId, long activityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static User Runner(bool optedIn = true, bool writeGranted = true) =>
        new() { Id = Guid.NewGuid(), StravaAthleteId = 1, SpotifyConnected = true, StravaWriteGranted = writeGranted, WriteSongsToStravaDescription = optedIn };

    private static StravaActivity Activity(string? description = null) => new() { Id = 55, Name = "Morning Run", Description = description };

    private static RunSong Song(string name, params string[] artists) => new() { Name = name, Artists = artists };

    private static readonly RunSong[] TwoSongs = [Song("Song One", "Artist A", "Artist B"), Song("Song Two", "Artist C")];

    private static readonly string TwoSongsBlock = $"{Start}\n1. Song One - Artist A, Artist B\n2. Song Two - Artist C\n{End}";

    private static Task Write(RecordingStrava strava, User user, StravaActivity activity, params RunSong[] songs) =>
        new StravaDescriptionWriter(strava, NullLogger<StravaDescriptionWriter>.Instance).WriteAsync(user, activity, songs);

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task With_the_setting_on_and_the_grant_the_songs_are_written_as_a_delimited_block()
    {
        var strava = new RecordingStrava();

        await Write(strava, Runner(), Activity(), TwoSongs);

        Assert.Equal((55L, TwoSongsBlock), Assert.Single(strava.Updates));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task With_the_setting_off_nothing_is_written()
    {
        var strava = new RecordingStrava();

        await Write(strava, Runner(optedIn: false), Activity(), TwoSongs);

        Assert.Empty(strava.Updates);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task Without_the_write_grant_nothing_is_written_even_if_the_setting_is_on()
    {
        var strava = new RecordingStrava();

        await Write(strava, Runner(writeGranted: false), Activity(), TwoSongs);

        Assert.Empty(strava.Updates);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task What_the_runner_wrote_is_kept_and_the_block_goes_after_it()
    {
        var strava = new RecordingStrava();

        await Write(strava, Runner(), Activity("Felt great today.\nNew PB!\n"), TwoSongs);

        Assert.Equal($"Felt great today.\nNew PB!\n\n{TwoSongsBlock}", Assert.Single(strava.Updates).Description);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task An_existing_Tracks_block_is_replaced_and_the_text_around_it_is_untouched()
    {
        var strava = new RecordingStrava();
        var existing = $"Before.\n\n{Start}\n1. Old - Someone\n{End}\n\nAfter.";

        await Write(strava, Runner(), Activity(existing), TwoSongs);

        var written = Assert.Single(strava.Updates).Description;
        Assert.Equal($"Before.\n\n{TwoSongsBlock}\n\nAfter.", written);
        Assert.Equal(1, written.Split(Start).Length - 1);
        Assert.DoesNotContain("Old - Someone", written);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task Processing_the_same_songs_again_writes_nothing_more()
    {
        var strava = new RecordingStrava();

        await Write(strava, Runner(), Activity($"Great run.\n\n{TwoSongsBlock}"), TwoSongs);

        Assert.Empty(strava.Updates);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task A_run_with_no_songs_writes_nothing()
    {
        var strava = new RecordingStrava();

        await Write(strava, Runner(), Activity("Great run."));

        Assert.Empty(strava.Updates);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SHARE-2")]
    public async Task A_Strava_error_is_swallowed_so_the_pipeline_carries_on()
    {
        var strava = new RecordingStrava(fails: true);

        await Write(strava, Runner(), Activity(), TwoSongs); // must not throw

        Assert.Single(strava.Updates);
    }
}
