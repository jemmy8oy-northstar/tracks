using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.Abstractions.Services;

public interface IGarminClient
{
    /// <summary>
    /// The running activities (any typeKey containing "running") that started on or after
    /// <paramref name="since"/>. Any failure throws; a 401 also forgets the cached token.
    /// </summary>
    Task<IReadOnlyList<IGarminRun>> SearchRunsAsync(DateOnly since, CancellationToken cancellationToken = default);

    /// <summary>The activity's original file: a ZIP holding the .fit. Any failure throws.</summary>
    Task<byte[]> DownloadAsync(long activityId, CancellationToken cancellationToken = default);
}
