using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

public static class RunContextMajority
{
    /// <summary>
    /// A context wins only when strictly more than half of ALL the songs share its uri (songs with no
    /// context count against it, and exactly half is not most). With no songs, or none that had a
    /// context, there is nothing to say; otherwise the songs were mixed.
    /// </summary>
    public static RunContextPick Pick(IReadOnlyList<IRunSong> songs)
    {
        var withContext = songs.Where(s => s.ContextUri is not null && s.ContextType is not null).ToList();
        if (withContext.Count == 0)
            return new RunContextPick(null, null);

        var top = withContext.GroupBy(s => s.ContextUri).OrderByDescending(g => g.Count()).First();
        return top.Count() * 2 > songs.Count
            ? new RunContextPick(top.First().ContextType, top.Key)
            : new RunContextPick(RunContextPick.Mixed, null);
    }
}
