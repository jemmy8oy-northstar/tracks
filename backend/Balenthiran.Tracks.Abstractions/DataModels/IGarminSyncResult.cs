namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>What one Garmin pass did.</summary>
public interface IGarminSyncResult
{
    /// <summary>Runs downloaded and kept.</summary>
    int Imported { get; }

    /// <summary>Runs left alone: already kept, or a file that was refused.</summary>
    int Skipped { get; }
}
