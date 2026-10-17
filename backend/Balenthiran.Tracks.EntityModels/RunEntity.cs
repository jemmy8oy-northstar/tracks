namespace Balenthiran.Tracks.EntityModels;

public class RunEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public long ActivityId { get; set; }

    public required string Name { get; set; }

    public required string SportType { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public int ElapsedSeconds { get; set; }

    public List<RunSongEntity> Songs { get; set; } = [];
}
