using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

/// <summary>Where workouts and the listening history come from (seeded demo data for now).</summary>
public interface IWorkoutSource
{
    IReadOnlyList<IWorkout> GetWorkouts();
    IReadOnlyList<ISongPlay> GetListeningHistory();
}
