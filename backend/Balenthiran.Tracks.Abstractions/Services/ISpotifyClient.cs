using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface ISpotifyClient
{
    /// <summary>Reads up to 50 of the user's plays that ended after <paramref name="after"/>.</summary>
    Task<IReadOnlyList<ISpotifyPlay>> GetRecentlyPlayedAsync(Guid userId, DateTimeOffset after, CancellationToken cancellationToken = default);
}
