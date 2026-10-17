namespace Balenthiran.Tracks.DataModels.Models;

/// <summary>JSON shape of GET /api/me.</summary>
public sealed record MeResponse(long AthleteId, bool SpotifyConnected);
