namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>Strava app configuration (section "Strava"). Never log any of these values.</summary>
public interface IStravaSettings
{
    string? ClientId { get; }
    string? ClientSecret { get; }
    string? VerifyToken { get; }

    /// <summary>Sport types that count as a run. Falls back to Run, TrailRun and VirtualRun when unset.</summary>
    IReadOnlyList<string> RunSportTypes { get; }

    /// <summary>True when the app credentials needed to call the Strava API are present.</summary>
    bool IsConfigured { get; }
}
