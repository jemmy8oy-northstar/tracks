using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface ISpotifyClient
{
    /// <summary>Reads up to 50 plays that ended after <paramref name="after"/>.</summary>
    Task<IReadOnlyList<ISpotifyPlay>> GetRecentlyPlayedAsync(DateTimeOffset after, CancellationToken cancellationToken = default);
}
