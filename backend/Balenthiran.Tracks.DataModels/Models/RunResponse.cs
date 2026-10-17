namespace Balenthiran.Tracks.DataModels.Models;

/// <summary>JSON shape of one run in GET /api/runs.</summary>
public sealed record RunResponse(
    long ActivityId,
    string Name,
    string SportType,
    DateTimeOffset StartedAt,
    int ElapsedSeconds,
    IReadOnlyList<RunSongResponse> Songs);
