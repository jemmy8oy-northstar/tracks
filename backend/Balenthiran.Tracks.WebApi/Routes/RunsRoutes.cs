using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Balenthiran.Tracks.WebApi.Routes;

public static class RunsRoutes
{
    public static RouteGroupBuilder MapRunsRoutes(this RouteGroupBuilder parentGroup)
    {
        var group = parentGroup.MapGroup("/runs");

        group.MapGet("", GetRuns).WithName("GetRuns");

        return parentGroup;
    }

    private static Ok<List<RunResponse>> GetRuns(IRunStore store)
    {
        var runs = store.GetAll().Select(run => new RunResponse(
            run.ActivityId,
            run.Name,
            run.SportType,
            run.StartedAt,
            run.ElapsedSeconds,
            run.Songs.Select(song => new RunSongResponse(
                song.Name, song.Artists, song.AlbumArtUrl, song.PlayedTo, song.OffsetSeconds)).ToList()));

        return TypedResults.Ok(runs.ToList());
    }
}
