namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>A finished run kept together with the songs played during it.</summary>
public interface IRun
{
    long ActivityId { get; }
    string Name { get; }
    string SportType { get; }
    DateTimeOffset StartedAt { get; }
    int ElapsedSeconds { get; }
    IReadOnlyList<IRunSong> Songs { get; }
}
