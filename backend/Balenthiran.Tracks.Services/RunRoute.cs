using System.Text.Json;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

/// <summary>A run's route (BEH-MAP-1/2): thinned from Strava's streams, kept as JSON, and where each song's pin goes.</summary>
public static class RunRoute
{
    /// <summary>Enough to draw a run on a phone; a 5k sampled every second would otherwise be ~1,800 points.</summary>
    public const int MaxPoints = 300;

    /// <summary>The route from the streams, evenly thinned to at most <paramref name="maxPoints"/>, always keeping the first and last; empty with no GPS.</summary>
    public static IReadOnlyList<IRoutePoint> FromStreams(IActivityStreams? streams, int maxPoints = MaxPoints)
    {
        if (streams is null || streams.LatLng.Count < 2 || streams.LatLng.Count != streams.TimeSeconds.Count)
            return [];

        var count = streams.LatLng.Count;
        var take = Math.Min(count, Math.Max(2, maxPoints));
        var points = new List<IRoutePoint>(take);
        for (var k = 0; k < take; k++)
        {
            var i = (int)Math.Round((double)k * (count - 1) / (take - 1));
            points.Add(new RoutePoint(streams.LatLng[i][0], streams.LatLng[i][1], streams.TimeSeconds[i]));
        }

        return points;
    }

    /// <summary>The index of the point whose time is nearest <paramref name="seconds"/> (BEH-MAP-2); null for an empty route. A tie goes to the earlier point.</summary>
    public static int? NearestIndex(IReadOnlyList<IRoutePoint> route, double seconds)
    {
        if (route.Count == 0)
            return null;

        var best = 0;
        for (var i = 1; i < route.Count; i++)
        {
            if (Math.Abs(route[i].Seconds - seconds) < Math.Abs(route[best].Seconds - seconds))
                best = i;
        }

        return best;
    }

    /// <summary>
    /// The distance covered along the route between two moments of the run, in metres (BEH-MAP-1); null
    /// without a route or when the window misses it. Each end is interpolated between the points around it.
    /// </summary>
    public static double? DistanceMetres(IReadOnlyList<IRoutePoint> route, double fromSeconds, double toSeconds)
    {
        if (route.Count < 2)
            return null;

        var from = Math.Max(fromSeconds, route[0].Seconds);
        var to = Math.Min(toSeconds, route[^1].Seconds);
        if (to <= from)
            return null;

        return DistanceAt(route, to) - DistanceAt(route, from);
    }

    /// <summary>Cumulative distance from the start of the route to a moment, interpolated in time within its segment.</summary>
    private static double DistanceAt(IReadOnlyList<IRoutePoint> route, double seconds)
    {
        var total = 0d;
        for (var i = 1; i < route.Count; i++)
        {
            var segment = Haversine(route[i - 1], route[i]);
            if (route[i].Seconds >= seconds)
            {
                var span = route[i].Seconds - route[i - 1].Seconds;
                return total + (span <= 0 ? 0 : segment * (seconds - route[i - 1].Seconds) / span);
            }

            total += segment;
        }

        return total;
    }

    private static double Haversine(IRoutePoint a, IRoutePoint b)
    {
        const double earthRadiusMetres = 6_371_000;
        static double Radians(double degrees) => degrees * Math.PI / 180;
        var dLat = Radians(b.Lat - a.Lat);
        var dLng = Radians(b.Lng - a.Lng);
        var h = Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Cos(Radians(a.Lat)) * Math.Cos(Radians(b.Lat)) * Math.Pow(Math.Sin(dLng / 2), 2);
        return 2 * earthRadiusMetres * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    public static string? Encode(IReadOnlyList<IRoutePoint> route) =>
        route.Count == 0 ? null : JsonSerializer.Serialize(route.Select(p => new[] { p.Lat, p.Lng, p.Seconds }));

    /// <summary>The stored route; empty for null or anything unreadable, so a bad row costs the map, never the run.</summary>
    public static IReadOnlyList<IRoutePoint> Decode(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return [];

        try
        {
            return (JsonSerializer.Deserialize<double[][]>(json) ?? [])
                .Where(p => p.Length == 3)
                .Select(p => (IRoutePoint)new RoutePoint(p[0], p[1], p[2]))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>The songs with the highlight picks applied: only the picked indices are flagged.</summary>
    public static IReadOnlyList<IRunSong> WithPicks(IReadOnlyList<IRunSong> songs, IRunHighlightPicks picks) =>
        songs.Select((song, index) => (IRunSong)new RunSong
        {
            Name = song.Name,
            Artists = song.Artists,
            AlbumArtUrl = song.AlbumArtUrl,
            PlayedFrom = song.PlayedFrom,
            PlayedTo = song.PlayedTo,
            OffsetSeconds = song.OffsetSeconds,
            IsTopSpeed = picks.TopSpeedIndex == index,
            IsFastestPace = picks.FastestPaceIndex == index,
            ContextType = song.ContextType,
            ContextUri = song.ContextUri,
        }).ToList();
}
