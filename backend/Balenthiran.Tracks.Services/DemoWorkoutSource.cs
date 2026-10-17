using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

/// <summary>
/// Seeded, entirely fictional demo data: an outdoor run around an imaginary park and an indoor
/// ride with no GPS, plus a listening history that also holds plays outside either workout.
/// No Strava or Spotify calls; covers are plain colours.
/// </summary>
public class DemoWorkoutSource : IWorkoutSource
{
    private static readonly DateTimeOffset RunStart = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RideStart = new(2026, 10, 11, 18, 0, 0, TimeSpan.Zero);

    private readonly IReadOnlyList<IWorkout> workouts;
    private readonly IReadOnlyList<ISongPlay> history;

    public DemoWorkoutSource()
    {
        workouts =
        [
            new Workout
            {
                Id = "park-run",
                Name = "Morning loop of Elm Park",
                StartedAt = RunStart,
                EndedAt = RunStart.AddMinutes(30),
                Route = BuildParkRoute(),
            },
            new Workout
            {
                Id = "indoor-ride",
                Name = "Indoor ride",
                StartedAt = RideStart,
                EndedAt = RideStart.AddMinutes(45),
            },
        ];

        history =
        [
            Play("Quiet Kettle", "Hollow Hours", "#64748b", RunStart.AddMinutes(-4), 200),
            Play("Tempo Lines", "The Stride Club", "#e11d48", RunStart, 215),
            Play("Park Lights", "Marlow & Finch", "#f59e0b", RunStart.AddSeconds(215), 265),
            Play("Second Wind", "Nova Lane", "#10b981", RunStart.AddSeconds(480), 220),
            Play("Gravel Hymn", "Old Rowan", "#6366f1", RunStart.AddSeconds(700), 260),
            Play("Mile Marker", "Hollow Hours", "#0ea5e9", RunStart.AddSeconds(960), 230),
            Play("Cold Air", "Nova Lane", "#a855f7", RunStart.AddSeconds(1190), 250),
            Play("Final Bend", "The Stride Club", "#f43f5e", RunStart.AddSeconds(1440), 210),
            Play("Home Straight", "Marlow & Finch", "#14b8a6", RunStart.AddSeconds(1650), 240),
            Play("Cooldown Tea", "Quiet Kettle", "#94a3b8", RunStart.AddMinutes(31), 180),
            Play("Spin Cycle", "Gear & Gale", "#f97316", RideStart, 240),
            Play("Cadence", "Nova Lane", "#22c55e", RideStart.AddSeconds(240), 300),
            Play("Saddle Sore", "Old Rowan", "#ef4444", RideStart.AddSeconds(540), 280),
            Play("Flat Road Blues", "Hollow Hours", "#3b82f6", RideStart.AddSeconds(1500), 320),
            Play("Last Interval", "The Stride Club", "#eab308", RideStart.AddSeconds(2400), 260),
        ];
    }

    public IReadOnlyList<IWorkout> GetWorkouts() => workouts;

    public IReadOnlyList<ISongPlay> GetListeningHistory() => history;

    private static SongPlay Play(string title, string artist, string colour, DateTimeOffset startedAt, int seconds) =>
        new()
        {
            Title = title,
            Artist = artist,
            CoverColour = colour,
            StartedAt = startedAt,
            DurationSeconds = seconds,
        };

    /// <summary>One and a quarter laps of an elliptical path, a fix every 30 seconds for 30 minutes.</summary>
    private static List<IRoutePoint> BuildParkRoute()
    {
        const int fixes = 60;
        var points = new List<IRoutePoint>(fixes + 1);
        for (var i = 0; i <= fixes; i++)
        {
            var angle = 1.25 * 2 * Math.PI * i / fixes;
            points.Add(new RoutePoint
            {
                Latitude = 51.5000 + 0.0030 * Math.Sin(angle),
                Longitude = -0.1500 + 0.0110 * Math.Cos(angle),
                Time = RunStart.AddSeconds(30 * i),
            });
        }

        return points;
    }
}
