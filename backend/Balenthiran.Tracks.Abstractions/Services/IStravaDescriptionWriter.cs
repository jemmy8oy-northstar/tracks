using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

/// <summary>BEH-SHARE-2: writes a run's songs into its Strava activity description, for runners who opted in.</summary>
public interface IStravaDescriptionWriter
{
    /// <summary>
    /// Does nothing unless the runner turned the option on and Strava granted <c>activity:write</c>. Otherwise adds a
    /// delimited Tracks block to the description (or replaces the one already there), leaving the runner's own text
    /// alone, and writes only when that changes it. A failure is logged and swallowed: it must never cost the run.
    /// </summary>
    Task WriteAsync(IUser user, IStravaActivity activity, IReadOnlyList<IRunSong> songs, CancellationToken cancellationToken = default);
}
