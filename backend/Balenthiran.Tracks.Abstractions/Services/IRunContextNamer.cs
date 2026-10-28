using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IRunContextNamer
{
    /// <summary>
    /// What most of a run's songs came from (BEH-HIGH-2) and, when Spotify will say, its name. Never throws for a name
    /// that cannot be read: the type is kept and the name left null.
    /// </summary>
    Task<(string? Type, string? Name)> NameAsync(Guid userId, IReadOnlyList<IRunSong> songs, CancellationToken cancellationToken = default);
}
