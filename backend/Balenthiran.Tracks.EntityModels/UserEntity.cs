namespace Balenthiran.Tracks.EntityModels;

/// <summary>A person who signed in with Strava. Never log or return the refresh tokens.</summary>
public class UserEntity
{
    public Guid Id { get; set; }

    public long StravaAthleteId { get; set; }

    public required string StravaRefreshToken { get; set; }

    public string? SpotifyRefreshToken { get; set; }

    /// <summary>The scopes Strava granted at the last sign-in, comma separated. Null for a runner who signed in before Tracks kept it (read-only then).</summary>
    public string? StravaScope { get; set; }

    /// <summary>BEH-SHARE-2: the runner opted in to the songs being written into their Strava activity descriptions. Off by default.</summary>
    public bool WriteSongsToStravaDescription { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<RunEntity> Runs { get; set; } = [];
}
