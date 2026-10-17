namespace Balenthiran.Tracks.Abstractions.DataModels;

public interface IWorkoutSummary
{
    string Id { get; set; }
    string Name { get; set; }
    DateTimeOffset StartedAt { get; set; }
    bool HasRoute { get; set; }
}
