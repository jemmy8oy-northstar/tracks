using Balenthiran.Tracks.Abstractions.DataModels;

namespace Balenthiran.Tracks.DataModels.Models;

public class SongPlay : ISongPlay
{
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required string CoverColour { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int DurationSeconds { get; set; }
}
