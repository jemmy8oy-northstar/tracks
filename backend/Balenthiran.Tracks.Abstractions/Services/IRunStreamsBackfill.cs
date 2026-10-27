namespace Balenthiran.Tracks.Abstractions.Services;

/// <summary>Fills in highlights and the route for runs kept before their streams were read (BEH-SYNC-6).</summary>
public interface IRunStreamsBackfill
{
    /// <summary>
    /// Reads up to <paramref name="limit"/> runs' streams, waiting <paramref name="spacing"/> between Strava calls.
    /// A run whose streams fail is added to <paramref name="skip"/>, so a bad one is not retried in a loop.
    /// Returns how many runs were filled in.
    /// </summary>
    Task<int> RunOnceAsync(int limit, TimeSpan spacing, ISet<long> skip, CancellationToken cancellationToken = default);
}
