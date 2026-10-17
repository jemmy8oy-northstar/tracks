using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.Services;

/// <summary>In-memory until there is a database: a restart forgets every run.</summary>
public class RunStore : IRunStore
{
    private readonly Lock gate = new();
    private readonly Dictionary<long, IRun> runs = [];

    public bool Save(IRun run)
    {
        lock (gate)
            return runs.TryAdd(run.ActivityId, run);
    }

    public IReadOnlyList<IRun> GetAll()
    {
        lock (gate)
            return runs.Values.OrderByDescending(r => r.StartedAt).ToList();
    }
}
