namespace Balenthiran.Tracks.DataModels.Models;

/// <summary>The context a run's songs mostly came from. <see cref="Type"/> is null for nothing, "mixed" when no context held a majority.</summary>
public sealed record RunContextPick(string? Type, string? Uri)
{
    public const string Mixed = "mixed";
}
