using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>Hands out a fixed token for whichever provider interface a client asks for, remembering for whom.</summary>
public sealed class StubTokenProvider : IStravaTokenProvider, ISpotifyTokenProvider
{
    public List<Guid> Users { get; } = [];

    public Task<string> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        Users.Add(userId);
        return Task.FromResult("stub-token");
    }
}
