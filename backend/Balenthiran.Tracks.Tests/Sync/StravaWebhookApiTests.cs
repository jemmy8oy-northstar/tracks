using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>The webhook and runs routes against the real host: routing, binding, status codes, the worker.</summary>
public class StravaWebhookApiTests
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(300);

    private static StringContent Body(string objectType = "activity", string aspect = "create", long owner = SyncApiFactory.AthleteId, long id = 99) =>
        new($$$"""{"object_type":"{{{objectType}}}","object_id":{{{id}}},"aspect_type":"{{{aspect}}}","owner_id":{{{owner}}},"updates":{}}""",
            Encoding.UTF8, "application/json");

    [Fact]
    public async Task The_handshake_echoes_the_challenge_for_the_right_token()
    {
        using var factory = new SyncApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/strava/webhook?hub.mode=subscribe&hub.verify_token={SyncApiFactory.VerifyToken}&hub.challenge=abc123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("abc123", payload!["hub.challenge"]);
    }

    [Theory]
    [InlineData("subscribe", "wrong")]
    [InlineData("subscribe", "")]
    [InlineData("unsubscribe", SyncApiFactory.VerifyToken)]
    public async Task The_handshake_is_refused_with_403_otherwise(string mode, string token)
    {
        using var factory = new SyncApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/strava/webhook?hub.mode={mode}&hub.verify_token={token}&hub.challenge=abc123");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("abc123", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_new_activity_is_acknowledged_and_then_processed_in_the_background()
    {
        using var factory = new SyncApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/strava/webhook", Body());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(99L, factory.Processor.Next(Wait));
    }

    [Fact]
    public async Task A_duplicate_delivery_is_processed_once()
    {
        using var factory = new SyncApiFactory();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/strava/webhook", Body())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/strava/webhook", Body())).StatusCode);

        Assert.Equal(99L, factory.Processor.Next(Wait));
        Assert.Null(factory.Processor.Next(Quiet));
    }

    [Theory]
    [InlineData("athlete", "create", SyncApiFactory.AthleteId)]
    [InlineData("activity", "update", SyncApiFactory.AthleteId)]
    [InlineData("activity", "delete", SyncApiFactory.AthleteId)]
    [InlineData("activity", "create", 1L)]
    public async Task Events_that_do_not_count_are_acknowledged_but_never_processed(string objectType, string aspect, long owner)
    {
        using var factory = new SyncApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/strava/webhook", Body(objectType, aspect, owner));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(factory.Processor.Next(Quiet));
    }

    [Fact]
    public async Task Runs_are_listed_newest_first_with_their_songs()
    {
        using var factory = new SyncApiFactory();
        var store = factory.Services.GetRequiredService<IRunStore>();
        var older = new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);
        store.Save(new Run { ActivityId = 1, Name = "older", StartedAt = older });
        store.Save(new Run
        {
            ActivityId = 2,
            Name = "newer",
            SportType = "Run",
            StartedAt = older.AddDays(1),
            ElapsedSeconds = 1800,
            Songs = [new RunSong { Name = "Song", Artists = ["A"], PlayedFrom = older, PlayedTo = older.AddMinutes(4), OffsetSeconds = 30 }],
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/runs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var runs = await response.Content.ReadFromJsonAsync<List<RunResponse>>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(["newer", "older"], runs!.Select(r => r.Name));
        var song = Assert.Single(runs![0].Songs);
        Assert.Equal("Song", song.Name);
        Assert.Equal(["A"], song.Artists);
        Assert.Equal(older.AddMinutes(4), song.PlayedAt);
        Assert.Equal(30, song.OffsetSeconds);
    }

    [Fact]
    public async Task With_no_runs_the_list_is_empty()
    {
        using var factory = new SyncApiFactory();
        var client = factory.CreateClient();

        Assert.Equal("[]", await client.GetStringAsync("/api/runs"));
    }
}
