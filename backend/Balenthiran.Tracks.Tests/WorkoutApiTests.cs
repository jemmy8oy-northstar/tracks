using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests;

/// <summary>The demo workouts, served read-only through the real host (BEH-MAP-1 / BEH-MAP-3 data).</summary>
public class WorkoutApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    [Trait("Behaviour", "BEH-MAP-1")]
    public async Task BEH_MAP_1_the_outdoor_demo_workout_is_served_with_a_route_and_pinned_songs()
    {
        using var factory = new StatusApiFactory();

        var map = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/workouts/park-run", Json);

        Assert.True(map.GetProperty("hasRoute").GetBoolean());
        Assert.True(map.GetProperty("route").GetArrayLength() > 10);
        var songs = map.GetProperty("songs");
        Assert.Equal(8, songs.GetArrayLength());
        foreach (var song in songs.EnumerateArray())
        {
            Assert.Equal(JsonValueKind.Object, song.GetProperty("pin").ValueKind);
            Assert.True(song.GetProperty("distanceMetres").GetDouble() > 0);
            Assert.False(string.IsNullOrWhiteSpace(song.GetProperty("coverColour").GetString()));
        }
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-2")]
    public async Task BEH_MAP_2_demo_plays_outside_the_workout_window_are_not_served()
    {
        using var factory = new StatusApiFactory();

        var map = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/workouts/park-run", Json);

        var titles = map.GetProperty("songs").EnumerateArray().Select(s => s.GetProperty("title").GetString()).ToList();
        Assert.DoesNotContain("Quiet Kettle", titles);
        Assert.DoesNotContain("Cooldown Tea", titles);
        Assert.Contains("Tempo Lines", titles);
    }

    [Fact]
    [Trait("Behaviour", "BEH-MAP-3")]
    public async Task BEH_MAP_3_the_indoor_demo_workout_is_served_with_songs_and_no_route()
    {
        using var factory = new StatusApiFactory();

        var map = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/workouts/indoor-ride", Json);

        Assert.False(map.GetProperty("hasRoute").GetBoolean());
        Assert.Equal(0, map.GetProperty("route").GetArrayLength());
        Assert.Equal(5, map.GetProperty("songs").GetArrayLength());
        Assert.All(map.GetProperty("songs").EnumerateArray(), s => Assert.Equal(JsonValueKind.Null, s.GetProperty("pin").ValueKind));
    }

    [Fact]
    public async Task Listing_workouts_names_both_demo_workouts_and_whether_each_has_a_route()
    {
        using var factory = new StatusApiFactory();

        var list = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/workouts", Json);

        Assert.Equal(["park-run", "indoor-ride"], list.EnumerateArray().Select(w => w.GetProperty("id").GetString()));
        Assert.Equal([true, false], list.EnumerateArray().Select(w => w.GetProperty("hasRoute").GetBoolean()));
    }

    [Fact]
    public async Task An_unknown_workout_is_a_404()
    {
        using var factory = new StatusApiFactory();

        var response = await factory.CreateClient().GetAsync("/api/workouts/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void The_demo_listening_history_includes_plays_outside_every_workout_window()
    {
        var source = new DemoWorkoutSource();
        var run = source.GetWorkouts().Single(w => w.Id == "park-run");

        Assert.Contains(source.GetListeningHistory(), p => p.StartedAt < run.StartedAt);
        Assert.Contains(source.GetListeningHistory(), p => p.StartedAt > run.EndedAt);
    }
}
