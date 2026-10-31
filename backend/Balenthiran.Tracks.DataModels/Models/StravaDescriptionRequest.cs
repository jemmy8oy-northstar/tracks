namespace Balenthiran.Tracks.DataModels.Models;

/// <summary>Body of PUT /api/me/strava-description: turn writing the songs into Strava descriptions on or off (BEH-SHARE-2).</summary>
public sealed record StravaDescriptionRequest(bool Enabled);
