using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>Stands in for the Strava push subscription, so a test host never calls Strava.</summary>
public sealed class StubSubscriptionService : IStravaSubscriptionService
{
    public string State => "subscribed";

    public Task<string> EnsureAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
}
