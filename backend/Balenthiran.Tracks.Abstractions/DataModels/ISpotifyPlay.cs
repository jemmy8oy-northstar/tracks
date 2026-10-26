namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>One entry of the Spotify recently-played list. <see cref="PlayedAt"/> is when the play ENDED.</summary>
public interface ISpotifyPlay
{
    string TrackId { get; }
    string TrackName { get; }
    IReadOnlyList<string> Artists { get; }
    string? AlbumArtUrl { get; }
    int DurationMs { get; }
    DateTimeOffset PlayedAt { get; }

    /// <summary>The kind of context the play came from (playlist, album, artist, show...); null when it had none, e.g. a queue.</summary>
    string? ContextType { get; }

    /// <summary>The context's uri (spotify:playlist:...); null when it had none.</summary>
    string? ContextUri { get; }
}
