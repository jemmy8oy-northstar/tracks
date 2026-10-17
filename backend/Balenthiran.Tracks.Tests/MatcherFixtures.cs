using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Tests;

/// <summary>
/// Hand-built workouts for the matcher tests. The route runs due east along the equator, one fix
/// every 10 seconds, each 0.001 degrees of longitude (about 111.19 m) past the last.
/// </summary>
public static class MatcherFixtures
{
    public static readonly DateTimeOffset Start = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    public const double MetresPerStep = 111.19;

    public static Workout Run(int fixes = 7, int seconds = 60) => new()
    {
        Id = "test-run",
        Name = "Test run",
        StartedAt = Start,
        EndedAt = Start.AddSeconds(seconds),
        Route = Line(fixes),
    };

    public static Workout Indoor(int seconds = 60) => new()
    {
        Id = "test-indoor",
        Name = "Test indoor",
        StartedAt = Start,
        EndedAt = Start.AddSeconds(seconds),
    };

    public static List<IRoutePoint> Line(int fixes) =>
        Enumerable.Range(0, fixes)
            .Select(i => (IRoutePoint)new RoutePoint
            {
                Latitude = 0,
                Longitude = i * 0.001,
                Time = Start.AddSeconds(i * 10),
            })
            .ToList();

    public static SongPlay Play(string title, int startOffsetSeconds, int durationSeconds = 600) => new()
    {
        Title = title,
        Artist = "Test Artist",
        CoverColour = "#112233",
        StartedAt = Start.AddSeconds(startOffsetSeconds),
        DurationSeconds = durationSeconds,
    };
}
