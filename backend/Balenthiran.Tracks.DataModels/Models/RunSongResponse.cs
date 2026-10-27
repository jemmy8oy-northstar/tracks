namespace Balenthiran.Tracks.DataModels.Models;

/// <summary>JSON shape of one song in GET /api/runs.</summary>
/// <param name="RouteIndex">The route point the song's pin sits on, the one nearest its start (BEH-MAP-2); null when the run has no route.</param>
/// <param name="DistanceMetres">How far the runner went while the song played, along the route (BEH-MAP-1); null when the run has no route.</param>
public sealed record RunSongResponse(
    string Name,
    IReadOnlyList<string> Artists,
    string? AlbumArtUrl,
    DateTimeOffset PlayedAt,
    int OffsetSeconds,
    bool IsTopSpeed,
    bool IsFastestPace,
    int? RouteIndex,
    double? DistanceMetres);
