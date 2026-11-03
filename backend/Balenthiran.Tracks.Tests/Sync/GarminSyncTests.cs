using System.Net;
using System.Text;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class GarminSyncTests
{
    private const string ApiKey = "the-personal-key";
    private static readonly DateTimeOffset Start = FitFixture.Start;
    private static readonly DateTimeOffset Now = Start.AddHours(2);

    private static string Base64Url(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>A JWT shaped like Garmin's: only the payload's exp matters.</summary>
    private static string Jwt(DateTimeOffset? expires, string marker = "a") =>
        $"{Base64Url("{\"alg\":\"none\"}")}.{Base64Url(expires is { } e ? $"{{\"exp\":{e.ToUnixTimeSeconds()},\"m\":\"{marker}\"}}" : "{}")}.sig";

    private static string Listed(params (long Id, DateTimeOffset At, string Type)[] items) =>
        "[" + string.Join(",", items.Select(i =>
            $"{{\"activityId\":{i.Id},\"startTimeGMT\":\"{i.At:yyyy-MM-dd HH:mm:ss}\",\"activityType\":{{\"typeKey\":\"{i.Type}\"}}}}")) + "]";

    private static GarminSettings Configured(Guid? user = null) => new() { ApiKey = ApiKey, UserId = (user ?? Guid.NewGuid()).ToString() };

    private static GarminClient Client(FakeHttpMessageHandler http, ManualTimeProvider clock, GarminSettings? settings = null) =>
        new(new HttpClient(http), settings ?? Configured(), clock);

    private static GarminSyncService Service(TestDb db, FakeHttpMessageHandler http, GarminSettings settings, ManualTimeProvider clock, RunUploadServiceTests.FakeSpotify spotify) =>
        new(settings, Client(http, clock, settings), new RunStore(db.Context()), RunUploadServiceTests.Build(db, spotify), clock, NullLogger<GarminSyncService>.Instance);

    private static async Task<IReadOnlyList<Abstractions.DataModels.IRun>> RunsOf(TestDb db, Guid userId) => await new RunStore(db.Context()).GetAllAsync(userId);

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-8")]
    public async Task The_key_is_traded_for_a_token_that_is_reused_until_an_hour_from_its_expiry()
    {
        var clock = new ManualTimeProvider(Now);
        var http = new FakeHttpMessageHandler()
            .Reply(Jwt(Now.AddHours(24)) + "\n").Reply("[]").Reply("[]")
            .Reply(Jwt(Now.AddHours(48), "b")).Reply("[]");
        var client = Client(http, clock);

        await client.SearchRunsAsync(new DateOnly(2026, 10, 15));
        clock.Advance(TimeSpan.FromHours(22));
        await client.SearchRunsAsync(new DateOnly(2026, 10, 15));
        Assert.Equal(3, http.Requests.Count);

        clock.Advance(TimeSpan.FromMinutes(90));
        await client.SearchRunsAsync(new DateOnly(2026, 10, 15));

        Assert.Equal(["POST", "GET", "GET", "POST", "GET"], http.Requests.Select(r => r.Method.Method));
        var exchange = http.Requests[0];
        Assert.Equal("ApiKey", exchange.Headers.Authorization?.Scheme);
        Assert.Equal(ApiKey, exchange.Headers.Authorization?.Parameter);
        Assert.EndsWith("/personalapitoken-service/personal-api/personal-api-key/token", exchange.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", http.Requests[1].Headers.Authorization?.Scheme);
        Assert.Equal(Jwt(Now.AddHours(24)), http.Requests[1].Headers.Authorization?.Parameter);
        Assert.Equal(Jwt(Now.AddHours(48), "b"), http.Requests[4].Headers.Authorization?.Parameter);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-8")]
    public async Task A_token_with_no_expiry_is_a_failure()
    {
        var http = new FakeHttpMessageHandler().Reply(Jwt(null));

        await Assert.ThrowsAsync<UpstreamServiceException>(() => Client(http, new ManualTimeProvider(Now)).SearchRunsAsync(new DateOnly(2026, 10, 15)));

        Assert.Single(http.Requests);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-8")]
    public async Task A_refused_token_is_dropped_so_the_next_call_trades_the_key_again()
    {
        var http = new FakeHttpMessageHandler()
            .Reply(Jwt(Now.AddHours(24))).Reply("", HttpStatusCode.Unauthorized)
            .Reply(Jwt(Now.AddHours(24), "b")).Reply("[]");
        var client = Client(http, new ManualTimeProvider(Now));

        await Assert.ThrowsAsync<UpstreamServiceException>(() => client.SearchRunsAsync(new DateOnly(2026, 10, 15)));
        await client.SearchRunsAsync(new DateOnly(2026, 10, 15));

        Assert.Equal(["POST", "GET", "POST", "GET"], http.Requests.Select(r => r.Method.Method));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-8")]
    public async Task Without_a_key_and_a_user_nothing_is_called()
    {
        using var db = new TestDb();
        var http = new FakeHttpMessageHandler();
        var clock = new ManualTimeProvider(Now);

        foreach (var settings in new[] { new GarminSettings(), new GarminSettings { ApiKey = ApiKey }, new GarminSettings { ApiKey = ApiKey, UserId = "not-a-guid" }, new GarminSettings { UserId = Guid.NewGuid().ToString() } })
        {
            Assert.False(settings.IsConfigured);
            var result = await Service(db, http, settings, clock, new RunUploadServiceTests.FakeSpotify()).SyncAsync();
            Assert.Equal((0, 0), (result.Imported, result.Skipped));
        }

        Assert.Empty(http.Requests);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-8")]
    public async Task A_new_garmin_run_is_downloaded_and_kept_for_the_configured_user_with_its_songs()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var settings = Configured(user.Id);
        var http = new FakeHttpMessageHandler()
            .Reply(Jwt(Now.AddHours(24))).Reply(Listed((21345678901, Start, "running")))
            .ReplyBytes(FitFixture.Zip(FitFixture.Fit()));
        var spotify = new RunUploadServiceTests.FakeSpotify(RunUploadServiceTests.Play("early", 8));

        var result = await Service(db, http, settings, new ManualTimeProvider(Now), spotify).SyncAsync();

        Assert.Equal((1, 0), (result.Imported, result.Skipped));
        var run = Assert.Single(await RunsOf(db, user.Id));
        Assert.Equal(Start, run.StartedAt);
        Assert.Equal(["early"], run.Songs.Select(s => s.Name));
        Assert.EndsWith("/download-service/files/activity/21345678901", http.Requests[2].RequestUri!.AbsolutePath);
        // Two days back from the clock, as a date.
        Assert.Contains("startDate=2026-10-15", http.Requests[1].RequestUri!.Query);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-8")]
    public async Task A_run_already_kept_at_that_start_is_not_downloaded_again()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        await new RunStore(db.Context()).SaveAsync(user.Id, new Run { ActivityId = 55, Name = "Morning Run", StartedAt = Start });
        var http = new FakeHttpMessageHandler().Reply(Jwt(Now.AddHours(24))).Reply(Listed((21345678901, Start, "running")));

        var result = await Service(db, http, Configured(user.Id), new ManualTimeProvider(Now), new RunUploadServiceTests.FakeSpotify()).SyncAsync();

        Assert.Equal((0, 1), (result.Imported, result.Skipped));
        Assert.Equal(2, http.Requests.Count);
        Assert.Equal("Morning Run", Assert.Single(await RunsOf(db, user.Id)).Name);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-8")]
    public async Task Activities_that_are_not_running_are_not_downloaded()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var http = new FakeHttpMessageHandler().Reply(Jwt(Now.AddHours(24))).Reply(Listed(
            (1, Start.AddHours(-30), "cycling"), (2, Start.AddHours(-20), "walking"), (3, Start, "treadmill_running")))
            .ReplyBytes(FitFixture.Zip(FitFixture.Fit(gps: false)));

        var result = await Service(db, http, Configured(user.Id), new ManualTimeProvider(Now), new RunUploadServiceTests.FakeSpotify()).SyncAsync();

        Assert.Equal(1, result.Imported);
        Assert.Equal(3, http.Requests.Count);
        Assert.EndsWith("/activity/3", http.Requests[2].RequestUri!.AbsolutePath);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-8")]
    public async Task A_list_item_missing_a_field_is_skipped_without_losing_the_rest()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var list = "[{\"activityId\":9,\"activityType\":{\"typeKey\":\"running\"}}," + Listed((3, Start, "running"))[1..];
        var http = new FakeHttpMessageHandler().Reply(Jwt(Now.AddHours(24))).Reply(list).ReplyBytes(FitFixture.Zip(FitFixture.Fit()));

        var result = await Service(db, http, Configured(user.Id), new ManualTimeProvider(Now), new RunUploadServiceTests.FakeSpotify()).SyncAsync();

        Assert.Equal(1, result.Imported);
        Assert.Single(await RunsOf(db, user.Id));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-8")]
    public async Task One_bad_file_does_not_stop_the_next_run_being_kept()
    {
        using var db = new TestDb();
        var user = await db.AddUserAsync(1);
        var http = new FakeHttpMessageHandler().Reply(Jwt(Now.AddHours(24)))
            .Reply(Listed((1, Start.AddDays(-1), "running"), (2, Start, "running")))
            .ReplyBytes("not a fit file"u8.ToArray()).ReplyBytes(FitFixture.Zip(FitFixture.Fit()));

        var result = await Service(db, http, Configured(user.Id), new ManualTimeProvider(Now), new RunUploadServiceTests.FakeSpotify()).SyncAsync();

        Assert.Equal((1, 1), (result.Imported, result.Skipped));
        Assert.Equal(Start, Assert.Single(await RunsOf(db, user.Id)).StartedAt);
    }
}
