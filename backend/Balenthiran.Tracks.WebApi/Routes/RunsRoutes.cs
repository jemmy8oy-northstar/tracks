using System.Security.Claims;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Balenthiran.Tracks.WebApi.Routes;

public static class RunsRoutes
{
    public static RouteGroupBuilder MapRunsRoutes(this RouteGroupBuilder parentGroup)
    {
        var group = parentGroup.MapGroup("/runs");

        group.MapGet("", GetRuns).RequireAuthorization().WithName("GetRuns");

        return parentGroup;
    }

    // Only ever the signed-in user's own runs: the id comes from the session, never from the request.
    private static async Task<Ok<List<RunResponse>>> GetRuns(ClaimsPrincipal principal, IRunStore store, CancellationToken cancellationToken)
    {
        var runs = (await store.GetAllAsync(principal.UserId(), cancellationToken)).Select(run => new RunResponse(
            run.ActivityId,
            run.Name,
            run.SportType,
            run.StartedAt,
            run.ElapsedSeconds,
            run.Songs.Select(song => new RunSongResponse(
                song.Name, song.Artists, song.AlbumArtUrl, song.PlayedTo, song.OffsetSeconds,
                song.IsTopSpeed, song.IsFastestPace, RunRoute.NearestIndex(run.Route, song.OffsetSeconds))).ToList(),
            run.ContextType,
            run.ContextName,
            run.Route.Select(p => new[] { p.Lat, p.Lng }).ToList()));

        return TypedResults.Ok(runs.ToList());
    }
}
