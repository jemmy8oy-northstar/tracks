namespace Balenthiran.Tracks.Abstractions.Services;

/// <summary>
/// Makes sure Strava's push subscription for this app points at our webhook. Strava allows one
/// per app, and without it no activity event is ever sent.
/// </summary>
public interface IStravaSubscriptionService
{
    /// <summary>The last outcome: pending, not-configured, subscribed, other-callback or failed.</summary>
    string State { get; }

    /// <summary>Creates the subscription when Strava has none. Never replaces one that points elsewhere.</summary>
    Task<string> EnsureAsync(CancellationToken cancellationToken = default);
}
