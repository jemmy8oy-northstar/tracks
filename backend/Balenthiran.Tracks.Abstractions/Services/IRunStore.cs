using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IRunStore
{
    /// <summary>Keeps the run. Returns false (and keeps the original) if the activity is already stored.</summary>
    bool Save(IRun run);

    /// <summary>All runs, newest first.</summary>
    IReadOnlyList<IRun> GetAll();
}
