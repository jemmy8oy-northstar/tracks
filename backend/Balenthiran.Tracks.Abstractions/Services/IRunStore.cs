using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IRunStore
{
    /// <summary>Keeps the run for the user. Returns false (and keeps the original) if they already have that activity.</summary>
    Task<bool> SaveAsync(Guid userId, IRun run, CancellationToken cancellationToken = default);

    /// <summary>That user's runs, newest first, each with its songs in play order.</summary>
    Task<IReadOnlyList<IRun>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
}
