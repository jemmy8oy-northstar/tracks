using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface ISongMatcher
{
    /// <summary>
    /// Places songs on a workout's route by matching when each started to where the runner was.
    /// Only plays that started inside the workout's window are kept.
    /// </summary>
    IMusicMap Match(IWorkout workout, IReadOnlyList<ISongPlay> plays);
}
