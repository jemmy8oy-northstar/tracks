namespace Balenthiran.Tracks.EntityModels;

/// <summary>A person who signed in with Strava. Never log or return the refresh tokens.</summary>
public class UserEntity
{
    public Guid Id { get; set; }

    public long StravaAthleteId { get; set; }

    public required string StravaRefreshToken { get; set; }

    public string? SpotifyRefreshToken { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<RunEntity> Runs { get; set; } = [];
}
