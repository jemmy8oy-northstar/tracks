using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface ISpotifyClient
{
    /// <summary>Reads up to 50 of the user's plays that ended after <paramref name="after"/>.</summary>
    Task<IReadOnlyList<ISpotifyPlay>> GetRecentlyPlayedAsync(Guid userId, DateTimeOffset after, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up the name of a play's context with the user's existing token (no extra scope):
    /// GET /v1/albums/{id}, /v1/playlists/{id}?fields=name or /v1/artists/{id}. Null when Spotify
    /// refuses or has no such thing (a private or editorial playlist) or the type has no lookup;
    /// an unreachable Spotify throws.
    /// </summary>
    Task<string?> GetContextNameAsync(Guid userId, string contextType, string contextUri, CancellationToken cancellationToken = default);
}
