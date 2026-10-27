using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

public class RunRouteTests
{
    private static ActivityStreams Streams(int samples) => new()
    {
        TimeSeconds = [.. Enumerable.Range(0, samples).Select(s => (double)s)],
        DistanceMetres = [.. Enumerable.Range(0, samples).Select(s => s * 3.0)],
        LatLng = [.. Enumerable.Range(0, samples).Select(s => (IReadOnlyList<double>)[51.5 + (s * 0.0001), -0.1])],
    };

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void A_long_route_is_thinned_evenly_and_keeps_its_first_and_last_points()
    {
        var route = RunRoute.FromStreams(Streams(1801), maxPoints: 300);

        Assert.Equal(300, route.Count);
        Assert.Equal(0, route[0].Seconds);
        Assert.Equal(1800, route[^1].Seconds);
        Assert.Equal(51.5 + (1800 * 0.0001), route[^1].Lat, 9);
        Assert.True(route.Zip(route.Skip(1)).All(pair => pair.Second.Seconds > pair.First.Seconds));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void A_short_route_is_kept_whole()
    {
        Assert.Equal([0d, 1, 2], RunRoute.FromStreams(Streams(3)).Select(p => p.Seconds));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-3")]
    public void No_GPS_gives_no_route()
    {
        Assert.Empty(RunRoute.FromStreams(null));
        Assert.Empty(RunRoute.FromStreams(new ActivityStreams { TimeSeconds = [0, 1], DistanceMetres = [0, 3] }));
        // Coordinates that do not line up with the times cannot be placed in time, so draw nothing.
        Assert.Empty(RunRoute.FromStreams(new ActivityStreams { TimeSeconds = [0, 1, 2], LatLng = [[51.5, -0.1], [51.6, -0.1]] }));
    }

    [Theory]
    [Trait("Behaviour", "BEH-MAP-2")]
    [InlineData(0, 0)]
    [InlineData(140, 0)]
    [InlineData(160, 1)]
    [InlineData(150, 0)] // a tie goes to the earlier point
    [InlineData(9999, 2)]
    [InlineData(-30, 0)]
    public void A_song_is_pinned_to_the_point_nearest_its_start(double seconds, int expected)
    {
        IReadOnlyList<IRoutePoint> route = [new RoutePoint(1, 1, 0), new RoutePoint(2, 2, 300), new RoutePoint(3, 3, 600)];

        Assert.Equal(expected, RunRoute.NearestIndex(route, seconds));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void An_empty_route_pins_nothing()
    {
        Assert.Null(RunRoute.NearestIndex([], 10));
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public void A_route_survives_being_stored_and_read_back()
    {
        IReadOnlyList<IRoutePoint> route = [new RoutePoint(51.5012345, -0.1234567, 0), new RoutePoint(51.6, -0.2, 12.5)];

        var back = RunRoute.Decode(RunRoute.Encode(route));

        Assert.Equal(route.Select(p => (p.Lat, p.Lng, p.Seconds)), back.Select(p => (p.Lat, p.Lng, p.Seconds)));
        Assert.Null(RunRoute.Encode([]));
    }

    [Theory]
    [Trait("Behaviour", "BEH-MAP-3")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"a\":1}")]
    public void A_missing_or_unreadable_stored_route_reads_as_none(string? json)
    {
        Assert.Empty(RunRoute.Decode(json));
    }
}
