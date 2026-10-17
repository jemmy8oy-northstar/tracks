using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests;

public class HaversineDistanceCalculatorTests
{
    private readonly HaversineDistanceCalculator calculator = new();

    [Fact]
    public void The_distance_from_a_point_to_itself_is_zero()
    {
        var p = new RoutePoint { Latitude = 51.5, Longitude = -0.15 };

        Assert.Equal(0, calculator.BetweenMetres(p, p), 6);
    }

    [Fact]
    public void One_thousandth_of_a_degree_of_longitude_on_the_equator_is_about_111_metres()
    {
        var a = new RoutePoint { Latitude = 0, Longitude = 0 };
        var b = new RoutePoint { Latitude = 0, Longitude = 0.001 };

        Assert.Equal(111.19, calculator.BetweenMetres(a, b), 1);
    }

    [Fact]
    public void The_distance_is_the_same_in_either_direction()
    {
        var a = new RoutePoint { Latitude = 51.5, Longitude = -0.15 };
        var b = new RoutePoint { Latitude = 51.51, Longitude = -0.14 };

        Assert.Equal(calculator.BetweenMetres(a, b), calculator.BetweenMetres(b, a), 6);
    }
}
