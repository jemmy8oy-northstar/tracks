namespace Balenthiran.Tracks.Abstractions.DataModels;

/// <summary>One entry in the listening history: a song and when it started playing.</summary>
public interface ISongPlay
{
    string Title { get; set; }
    string Artist { get; set; }

    /// <summary>CSS colour standing in for the cover art (no external images).</summary>
    string CoverColour { get; set; }

    DateTimeOffset StartedAt { get; set; }
    int DurationSeconds { get; set; }
}
