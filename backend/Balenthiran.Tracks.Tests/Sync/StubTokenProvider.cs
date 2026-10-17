using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>Hands out a fixed token for whichever provider interface a client asks for.</summary>
public sealed class StubTokenProvider : IStravaTokenProvider, ISpotifyTokenProvider
{
    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult("stub-token");
}
