using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunContextMajorityTests
{
    private static IRunSong Song(string? uri) => new RunSong
    {
        Name = "song",
        ContextType = uri?.Split(':')[1],
        ContextUri = uri,
    };

    private static RunContextPick Pick(params string?[] uris) => RunContextMajority.Pick([.. uris.Select(Song)]);

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public void Songs_all_from_one_context_name_it()
    {
        Assert.Equal(new RunContextPick("album", "spotify:album:a"), Pick("spotify:album:a", "spotify:album:a", "spotify:album:a"));
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public void More_than_half_from_one_context_name_it()
    {
        Assert.Equal(
            new RunContextPick("playlist", "spotify:playlist:p"),
            Pick("spotify:playlist:p", "spotify:playlist:p", "spotify:playlist:p", "spotify:album:a", "spotify:artist:r"));
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public void Exactly_half_is_mixed()
    {
        Assert.Equal(new RunContextPick("mixed", null), Pick("spotify:album:a", "spotify:album:a", "spotify:album:b", "spotify:album:b"));
        Assert.Equal(new RunContextPick("mixed", null), Pick("spotify:album:a", "spotify:album:b"));
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public void Songs_with_no_context_count_against_a_majority()
    {
        // 2 of 4 share the album, the other two came from a queue: not most of the run.
        Assert.Equal(new RunContextPick("mixed", null), Pick("spotify:album:a", "spotify:album:a", null, null));
        Assert.Equal(new RunContextPick("album", "spotify:album:a"), Pick("spotify:album:a", "spotify:album:a", null));
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public void No_song_with_a_context_says_nothing()
    {
        Assert.Equal(new RunContextPick(null, null), Pick(null, null));
    }

    [Fact]
    [Trait("Behaviour", "BEH-HIGH-2")]
    public void No_songs_say_nothing()
    {
        Assert.Equal(new RunContextPick(null, null), Pick());
    }
}
