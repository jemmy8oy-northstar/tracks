namespace Balenthiran.Tracks.DataModels.Models;

/// <summary>JSON shape of one run in GET /api/runs.</summary>
/// <param name="Route">The route as [lat, lng] pairs (BEH-MAP-1); empty when the run has no GPS or its streams are not read yet.</param>
public sealed record RunResponse(
    long ActivityId,
    string Name,
    string SportType,
    DateTimeOffset StartedAt,
    int ElapsedSeconds,
    IReadOnlyList<RunSongResponse> Songs,
    string? ContextType,
    string? ContextName,
    IReadOnlyList<double[]> Route);
