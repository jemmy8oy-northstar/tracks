using System.Net;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Balenthiran.Tracks.Tests.Sync;

public class StravaSubscriptionTests
{
    private const string Callback = "https://balenthiran.co.uk/tracks/api/strava/webhook";

    private static readonly StravaSettings Configured = new() { ClientId = "id", ClientSecret = "secret", VerifyToken = "verify" };

    private static StravaSubscriptionService Service(FakeHttpMessageHandler handler, StravaSettings? settings = null, string? baseUrl = "https://balenthiran.co.uk/tracks/") =>
        new(new HttpClient(handler), settings ?? Configured, baseUrl, NullLogger<StravaSubscriptionService>.Instance);

    [Fact]
    public async Task An_existing_subscription_to_our_webhook_is_left_alone()
    {
        var handler = new FakeHttpMessageHandler().Reply($$"""[{"id":1,"callback_url":"{{Callback}}"}]""");

        Assert.Equal("subscribed", await Service(handler).EnsureAsync());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://www.strava.com/api/v3/push_subscriptions?client_id=id&client_secret=secret", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task A_subscription_to_another_callback_is_reported_and_never_replaced()
    {
        var handler = new FakeHttpMessageHandler().Reply("""[{"id":1,"callback_url":"https://elsewhere.example/hook"}]""");
        var service = Service(handler);

        Assert.Equal("other-callback", await service.EnsureAsync());
        Assert.Single(handler.Requests);
        Assert.Equal("other-callback", service.State);
    }

    [Fact]
    public async Task With_none_it_creates_one_pointing_at_our_webhook()
    {
        var handler = new FakeHttpMessageHandler().Reply("[]").Reply("""{"id":7}""", HttpStatusCode.Created);

        Assert.Equal("subscribed", await Service(handler).EnsureAsync());

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal("https://www.strava.com/api/v3/push_subscriptions", handler.Requests[1].RequestUri!.ToString());
        Assert.Equal($"client_id=id&client_secret=secret&callback_url={Uri.EscapeDataString(Callback)}&verify_token=verify", handler.RequestBodies[1]);
    }

    [Fact]
    public async Task A_refused_create_is_a_failure()
    {
        var handler = new FakeHttpMessageHandler().Reply("[]").Reply("""{"errors":[{"code":"GET to callback URL does not return 200"}]}""", HttpStatusCode.BadRequest);

        Assert.Equal("failed", await Service(handler).EnsureAsync());
    }

    [Fact]
    public async Task A_failed_listing_is_a_failure_and_creates_nothing()
    {
        var handler = new FakeHttpMessageHandler().Reply("""{"message":"Authorization Error"}""", HttpStatusCode.Unauthorized);

        Assert.Equal("failed", await Service(handler).EnsureAsync());
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(null, "verify", "https://balenthiran.co.uk/tracks")]
    [InlineData("secret", null, "https://balenthiran.co.uk/tracks")]
    [InlineData("secret", "verify", null)]
    public async Task Unconfigured_it_calls_nothing(string? secret, string? verifyToken, string? baseUrl)
    {
        var handler = new FakeHttpMessageHandler();
        var settings = new StravaSettings { ClientId = "id", ClientSecret = secret, VerifyToken = verifyToken };

        Assert.Equal("not-configured", await Service(handler, settings, baseUrl).EnsureAsync());
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task The_worker_waits_for_the_app_to_start_then_retries_a_failure()
    {
        var subscription = new ScriptedSubscription("failed", "failed", "subscribed");
        var lifetime = new FakeLifetime();
        var worker = new StravaSubscriptionWorker(subscription, lifetime, NullLogger<StravaSubscriptionWorker>.Instance, [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero]);

        await worker.StartAsync(CancellationToken.None);
        Assert.Equal(0, subscription.Calls);

        lifetime.Start();
        await worker.ExecuteTask!;
        Assert.Equal(3, subscription.Calls);
    }

    [Fact]
    public async Task The_worker_gives_up_after_its_retries()
    {
        var subscription = new ScriptedSubscription("failed", "failed", "failed", "failed", "failed");
        var lifetime = new FakeLifetime();
        var worker = new StravaSubscriptionWorker(subscription, lifetime, NullLogger<StravaSubscriptionWorker>.Instance, [TimeSpan.Zero, TimeSpan.Zero]);

        lifetime.Start();
        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!;
        Assert.Equal(3, subscription.Calls);
    }

    private sealed class ScriptedSubscription(params string[] outcomes) : IStravaSubscriptionService
    {
        public int Calls { get; private set; }

        public string State { get; private set; } = "pending";

        public Task<string> EnsureAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(State = outcomes[Calls++]);
    }

    private sealed class FakeLifetime : IHostApplicationLifetime
    {
        private readonly CancellationTokenSource started = new();

        public CancellationToken ApplicationStarted => started.Token;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void Start() => started.Cancel();

        public void StopApplication()
        {
        }
    }
}
