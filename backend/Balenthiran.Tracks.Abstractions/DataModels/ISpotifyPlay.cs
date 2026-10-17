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
}
