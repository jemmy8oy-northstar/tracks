using System.Security.Claims;
using Balenthiran.Tracks.Abstractions.DataModels;
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
        // A multipart POST needs the cookie, which is SameSite=Lax and so never sent from another site; the antiforgery token adds nothing.
        group.MapPost("upload", UploadRun).DisableAntiforgery().RequireAuthorization().WithName("UploadRun");

        return parentGroup;
    }

    // Only ever the signed-in user's own runs: the id comes from the session, never from the request.
    private static async Task<Ok<List<RunResponse>>> GetRuns(ClaimsPrincipal principal, IRunStore store, CancellationToken cancellationToken)
    {
        var runs = (await store.GetAllAsync(principal.UserId(), cancellationToken)).Select(ToResponse);

        return TypedResults.Ok(runs.ToList());
    }

    // BEH-SYNC-7: a Garmin "Export Original" (.fit, or the .zip around it). The file is read and dropped, never stored.
    private static async Task<Ok<RunResponse>> UploadRun(
        IFormFile file, ClaimsPrincipal principal, IRunUploadService uploads, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var run = await uploads.UploadAsync(principal.UserId(), stream, cancellationToken);

        return TypedResults.Ok(ToResponse(run));
    }

    private static RunResponse ToResponse(IRun run) => new(
        run.ActivityId,
        run.Name,
        run.SportType,
        run.StartedAt,
        run.ElapsedSeconds,
        run.Songs.Select(song => new RunSongResponse(
            song.Name, song.Artists, song.AlbumArtUrl, song.PlayedTo, song.OffsetSeconds,
            song.IsTopSpeed, song.IsFastestPace, RunRoute.NearestIndex(run.Route, song.OffsetSeconds),
            RunRoute.DistanceMetres(run.Route, song.OffsetSeconds, song.OffsetSeconds + (song.PlayedTo - song.PlayedFrom).TotalSeconds))).ToList(),
        run.ContextType,
        run.ContextName,
        run.Route.Select(p => new[] { p.Lat, p.Lng }).ToList());
}
