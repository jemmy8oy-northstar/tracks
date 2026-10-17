using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

public class MusicMapService(IWorkoutSource source, ISongMatcher matcher) : IMusicMapService
{
    public Task<IReadOnlyList<IWorkoutSummary>> ListWorkoutsAsync()
    {
        IReadOnlyList<IWorkoutSummary> summaries = source.GetWorkouts()
            .OrderBy(w => w.StartedAt)
            .Select(w => (IWorkoutSummary)new WorkoutSummary
            {
                Id = w.Id,
                Name = w.Name,
                StartedAt = w.StartedAt,
                HasRoute = w.Route.Count > 0,
            })
            .ToList();
        return Task.FromResult(summaries);
    }

    public Task<IMusicMap> GetMusicMapAsync(string workoutId)
    {
        var workout = source.GetWorkouts().FirstOrDefault(w => w.Id == workoutId)
            ?? throw new NotFoundException($"No workout with id '{workoutId}'.");
        return Task.FromResult(matcher.Match(workout, source.GetListeningHistory()));
    }
}
