using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IMusicMapService
{
    Task<IReadOnlyList<IWorkoutSummary>> ListWorkoutsAsync();

    /// <exception cref="Exceptions.NotFoundException">No workout has this id.</exception>
    Task<IMusicMap> GetMusicMapAsync(string workoutId);
}
