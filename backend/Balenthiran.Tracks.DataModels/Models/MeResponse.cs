namespace Balenthiran.Tracks.DataModels.Models;

/// <summary>
/// JSON shape of GET /api/me. <paramref name="StravaWriteGranted"/> is whether Strava let Tracks edit activities
/// (false = the runner must reconnect Strava); <paramref name="WriteSongsToStravaDescription"/> is their opt-in (BEH-SHARE-2).
/// </summary>
public sealed record MeResponse(long AthleteId, bool SpotifyConnected, bool StravaWriteGranted, bool WriteSongsToStravaDescription);
