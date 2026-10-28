using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Microsoft.Extensions.Logging;

namespace Balenthiran.Tracks.Services;

// The context is a bonus too: a name Spotify will not give (a private or editorial playlist, a
// rate limit, no network) leaves the type without a name and never costs the run.
public class RunContextNamer(ISpotifyClient spotify, ILogger<RunContextNamer> logger) : IRunContextNamer
{
    public async Task<(string? Type, string? Name)> NameAsync(
        Guid userId, IReadOnlyList<IRunSong> songs, CancellationToken cancellationToken = default)
    {
        var pick = RunContextMajority.Pick(songs);
        if (pick.Uri is null)
            return (pick.Type, null);

        try
        {
            return (pick.Type, await spotify.GetContextNameAsync(userId, pick.Type!, pick.Uri, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Storing a run with its {ContextType} unnamed: the name could not be read.", pick.Type);
            return (pick.Type, null);
        }
    }
}
