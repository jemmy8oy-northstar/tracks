using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>The webhook and runs routes against the real host: routing, binding, status codes, the worker.</summary>
public class StravaWebhookApiTests
{
    private const long Athlete = 4242;
    private const long OtherAthlete = 9;

    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(300);
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static StringContent Body(string objectType = "activity", string aspect = "create", long owner = Athlete, long id = 99, string updates = "{}") =>
        new($$$"""{"object_type":"{{{objectType}}}","object_id":{{{id}}},"aspect_type":"{{{aspect}}}","owner_id":{{{owner}}},"updates":{{{updates}}}}""",
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
    public async Task A_new_activity_from_a_signed_up_athlete_is_acknowledged_and_then_processed_in_the_background()
    {
        using var factory = new SyncApiFactory();
        await SignIn.WithStravaAsync(factory, factory.Browser(), Athlete);
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/strava/webhook", Body());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((Athlete, 99L), factory.Processor.Next(Wait));
    }

    [Fact]
    public async Task A_duplicate_delivery_is_processed_once()
    {
        using var factory = new SyncApiFactory();
        await SignIn.WithStravaAsync(factory, factory.Browser(), Athlete);
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/strava/webhook", Body())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/strava/webhook", Body())).StatusCode);

        Assert.Equal((Athlete, 99L), factory.Processor.Next(Wait));
        Assert.Null(factory.Processor.Next(Quiet));
    }

    [Fact]
    public async Task An_event_for_an_unknown_athlete_is_acknowledged_but_never_processed()
    {
        using var factory = new SyncApiFactory();
        await SignIn.WithStravaAsync(factory, factory.Browser(), Athlete);
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/strava/webhook", Body(owner: OtherAthlete));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(factory.Processor.Next(Quiet));
    }

    [Theory]
    [InlineData("athlete", "create")]
    [InlineData("activity", "update")]
    [InlineData("activity", "delete")]
    public async Task Events_that_do_not_count_are_acknowledged_but_never_processed(string objectType, string aspect)
    {
        using var factory = new SyncApiFactory();
        await SignIn.WithStravaAsync(factory, factory.Browser(), Athlete);
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/strava/webhook", Body(objectType, aspect));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(factory.Processor.Next(Quiet));
    }

    [Fact]
    public async Task Revoking_access_on_Strava_deletes_the_user_and_ends_their_session()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);
        await SignIn.WithStravaAsync(factory, factory.Browser(), OtherAthlete);

        // Strava sends other kinds of values in `updates` too; a non-string must not break binding.
        var response = await factory.CreateClient().PostAsync(
            "/api/strava/webhook", Body("athlete", "update", id: Athlete, updates: """{"authorized":"false"}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OtherAthlete, Assert.Single(await factory.WithDb(db => db.Users.ToListAsync())).StravaAthleteId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Updates_with_values_that_are_not_strings_still_bind()
    {
        using var factory = new SyncApiFactory();
        await SignIn.WithStravaAsync(factory, factory.Browser(), Athlete);

        var response = await factory.CreateClient().PostAsync(
            "/api/strava/webhook", Body("activity", "update", updates: """{"title":"New","private":true,"n":3}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await factory.WithDb(db => db.Users.CountAsync()));
    }

    [Fact]
    public async Task Runs_need_a_session()
    {
        using var factory = new SyncApiFactory();

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Browser().GetAsync("/api/runs")).StatusCode);
    }

    [Fact]
    public async Task With_no_runs_the_list_is_empty()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);

        Assert.Equal("[]", await browser.GetStringAsync("/api/runs"));
    }

    [Fact]
    public async Task Each_user_sees_only_their_own_runs_newest_first_with_their_songs()
    {
        using var factory = new SyncApiFactory();
        var ann = factory.Browser();
        var bob = factory.Browser();
        await SignIn.WithStravaAsync(factory, ann, Athlete);
        await SignIn.WithStravaAsync(factory, bob, OtherAthlete);
        var older = new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);
        await Save(factory, Athlete, new Run { ActivityId = 1, Name = "ann older", StartedAt = older });
        await Save(factory, Athlete, new Run
        {
            ActivityId = 2,
            Name = "ann newer",
            SportType = "Run",
            StartedAt = older.AddDays(1),
            ElapsedSeconds = 1800,
            Songs = [new RunSong { Name = "Song", Artists = ["A"], PlayedFrom = older, PlayedTo = older.AddMinutes(4), OffsetSeconds = 30 }],
        });
        await Save(factory, OtherAthlete, new Run { ActivityId = 1, Name = "bob's", StartedAt = older });

        var annsRuns = await ann.GetFromJsonAsync<List<RunResponse>>("/api/runs", Web);
        var bobsRuns = await bob.GetFromJsonAsync<List<RunResponse>>("/api/runs", Web);

        Assert.Equal(["ann newer", "ann older"], annsRuns!.Select(r => r.Name));
        var song = Assert.Single(annsRuns![0].Songs);
        Assert.Equal("Song", song.Name);
        Assert.Equal(["A"], song.Artists);
        Assert.Equal(older.AddMinutes(4), song.PlayedAt);
        Assert.Equal(30, song.OffsetSeconds);
        Assert.Equal("bob's", Assert.Single(bobsRuns!).Name);
    }

    private static async Task Save(SyncApiFactory factory, long athleteId, Run run)
    {
        await factory.WithDb(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.StravaAthleteId == athleteId);
            return await new RunStore(db).SaveAsync(user.Id, run);
        });
    }
}
