using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Balenthiran.Tracks.WebApi.Routes;

public static class WorkoutRoutes
{
    public static RouteGroupBuilder MapWorkoutRoutes(this RouteGroupBuilder parentGroup)
    {
        var group = parentGroup.MapGroup("/workouts");

        group.MapGet("", ListWorkouts).WithName("ListWorkouts");
        group.MapGet("{id}", GetWorkoutMap).WithName("GetWorkoutMap");

        return parentGroup;
    }

    private static async Task<Ok<IReadOnlyList<IWorkoutSummary>>> ListWorkouts(IMusicMapService service) =>
        TypedResults.Ok(await service.ListWorkoutsAsync());

    // An unknown id makes the service throw NotFoundException; the global handler renders the 404.
    private static async Task<Ok<IMusicMap>> GetWorkoutMap(string id, IMusicMapService service) =>
        TypedResults.Ok(await service.GetMusicMapAsync(id));
}
