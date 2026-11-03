namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A running activity in Garmin's list: enough to decide whether to download it.</summary>
public interface IGarminRun
{
    long ActivityId { get; }

    DateTimeOffset StartedAt { get; }
}
