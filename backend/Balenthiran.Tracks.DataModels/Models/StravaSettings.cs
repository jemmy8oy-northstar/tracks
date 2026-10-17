using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class StravaSettings : IStravaSettings
{
    private static readonly string[] DefaultRunSportTypes = ["Run", "TrailRun", "VirtualRun"];

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RefreshToken { get; set; }
    public string? VerifyToken { get; set; }
    public long? AthleteId { get; set; }

    // Bound from config as an array; kept apart from RunSportTypes so configuring it replaces
    // the defaults rather than being appended to them.
    public string[]? SportTypes { get; set; }

    public IReadOnlyList<string> RunSportTypes =>
        SportTypes is { Length: > 0 } ? SportTypes : DefaultRunSportTypes;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(RefreshToken);
}
