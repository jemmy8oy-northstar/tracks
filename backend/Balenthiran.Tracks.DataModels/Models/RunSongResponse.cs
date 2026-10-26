namespace Balenthiran.Tracks.DataModels.Models;

/// <summary>JSON shape of one song in GET /api/runs.</summary>
public sealed record RunSongResponse(
    string Name,
    IReadOnlyList<string> Artists,
    string? AlbumArtUrl,
    DateTimeOffset PlayedAt,
    int OffsetSeconds,
    bool IsTopSpeed,
    bool IsFastestPace);
