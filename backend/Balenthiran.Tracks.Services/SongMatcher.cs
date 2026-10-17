using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

/// <summary>
/// BEH-MAP-2: a song is placed on the route by matching when it played to where the runner was.
/// </summary>
public class SongMatcher(IDistanceCalculator distances) : ISongMatcher
{
    public IMusicMap Match(IWorkout workout, IReadOnlyList<ISongPlay> plays)
    {
        var route = workout.Route.OrderBy(p => p.Time).ToList();
        var metresAlongRoute = CumulativeMetres(route);

        var inWindow = plays
            .Where(p => p.StartedAt >= workout.StartedAt && p.StartedAt <= workout.EndedAt)
            .OrderBy(p => p.StartedAt)
            .ToList();

        var songs = new List<ISongPin>(inWindow.Count);
        for (var i = 0; i < inWindow.Count; i++)
        {
            var play = inWindow[i];
            var endsAt = SongEnd(workout, play, i + 1 < inWindow.Count ? inWindow[i + 1] : null);

            var pin = new SongPin
            {
                Title = play.Title,
                Artist = play.Artist,
                CoverColour = play.CoverColour,
                StartedAt = play.StartedAt,
                ElapsedSeconds = (int)(play.StartedAt - workout.StartedAt).TotalSeconds,
            };

            if (route.Count > 0)
            {
                var start = NearestIndex(route, play.StartedAt);
                var end = NearestIndex(route, endsAt);
                pin.Pin = route[start];
                pin.DistanceMetres = Math.Max(0, metresAlongRoute[end] - metresAlongRoute[start]);
            }

            songs.Add(pin);
        }

        return new MusicMap
        {
            Id = workout.Id,
            Name = workout.Name,
            StartedAt = workout.StartedAt,
            EndedAt = workout.EndedAt,
            HasRoute = route.Count > 0,
            Route = route,
            Songs = songs,
        };
    }

    /// <summary>A song covers ground until it ends, the next one starts, or the workout ends.</summary>
    private static DateTimeOffset SongEnd(IWorkout workout, ISongPlay play, ISongPlay? next)
    {
        var end = play.StartedAt.AddSeconds(play.DurationSeconds);
        if (next is not null && next.StartedAt < end)
        {
            end = next.StartedAt;
        }

        return end > workout.EndedAt ? workout.EndedAt : end;
    }

    /// <summary>The route point whose timestamp is nearest <paramref name="moment"/>; the earlier point wins a tie.</summary>
    private static int NearestIndex(List<IRoutePoint> route, DateTimeOffset moment)
    {
        var best = 0;
        var bestGap = (route[0].Time - moment).Duration();
        for (var i = 1; i < route.Count; i++)
        {
            var gap = (route[i].Time - moment).Duration();
            if (gap < bestGap)
            {
                best = i;
                bestGap = gap;
            }
        }

        return best;
    }

    private List<double> CumulativeMetres(List<IRoutePoint> route)
    {
        var total = new List<double>(route.Count);
        var running = 0.0;
        for (var i = 0; i < route.Count; i++)
        {
            if (i > 0)
            {
                running += distances.BetweenMetres(route[i - 1], route[i]);
            }

            total.Add(running);
        }

        return total;
    }
}
