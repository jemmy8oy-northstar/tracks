namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>A clock the test moves by hand, so token expiry is exercised without waiting.</summary>
public sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset now = start;

    public void Advance(TimeSpan by) => now += by;

    public override DateTimeOffset GetUtcNow() => now;
}
