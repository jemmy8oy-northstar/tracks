namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>Strava configuration (section "Strava"). Never log any of these values.</summary>
public interface IStravaSettings
{
    string? ClientId { get; }
    string? ClientSecret { get; }
    string? RefreshToken { get; }
    string? VerifyToken { get; }
    long? AthleteId { get; }

    /// <summary>Sport types that count as a run. Falls back to Run, TrailRun and VirtualRun when unset.</summary>
    IReadOnlyList<string> RunSportTypes { get; }

    /// <summary>True when the credentials needed to call the Strava API are all present.</summary>
    bool IsConfigured { get; }
}
