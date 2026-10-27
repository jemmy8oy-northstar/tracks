using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IRunStore
{
    /// <summary>Keeps the run for the user. Returns false (and keeps the original) if they already have that activity.</summary>
    Task<bool> SaveAsync(Guid userId, IRun run, CancellationToken cancellationToken = default);

    /// <summary>That user's runs, newest first, each with its songs in play order.</summary>
    Task<IReadOnlyList<IRun>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Up to <paramref name="limit"/> runs, any user's, whose streams were never read (BEH-SYNC-6), newest first, leaving out <paramref name="skip"/>.</summary>
    Task<IReadOnlyList<(Guid UserId, IRun Run)>> GetUnreadStreamsAsync(int limit, IReadOnlyCollection<long> skip, CancellationToken cancellationToken = default);

    /// <summary>Records that the run's streams were read: its songs' highlight flags (by position) and its route.</summary>
    Task SaveStreamsAsync(Guid userId, long activityId, IReadOnlyList<IRunSong> songs, IReadOnlyList<IRoutePoint> route, CancellationToken cancellationToken = default);
}
