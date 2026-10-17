using System.Net;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;

namespace Balenthiran.Tracks.Tests.Sync;

public class TokenProviderTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 17, 10, 0, 0, TimeSpan.Zero);

    private static string StravaReply(string access, DateTimeOffset expiresAt, string refresh = "refresh-1") =>
        $$"""{"access_token":"{{access}}","refresh_token":"{{refresh}}","expires_at":{{expiresAt.ToUnixTimeSeconds()}}}""";

    private static string SpotifyReply(string access, int expiresIn = 3600, string? refresh = null) =>
        refresh is null
            ? $$"""{"access_token":"{{access}}","expires_in":{{expiresIn}}}"""
            : $$"""{"access_token":"{{access}}","expires_in":{{expiresIn}},"refresh_token":"{{refresh}}"}""";

    private static StravaTokenProvider Strava(FakeHttpMessageHandler handler, ManualTimeProvider time) =>
        new(new HttpClient(handler), new StravaSettings { ClientId = "id", ClientSecret = "shh", RefreshToken = "refresh-0" }, time);

    private static SpotifyTokenProvider Spotify(FakeHttpMessageHandler handler, ManualTimeProvider time) =>
        new(new HttpClient(handler), new SpotifySettings { ClientId = "id", ClientSecret = "shh", RefreshToken = "refresh-0" }, time);

    [Fact]
    public async Task Strava_exchanges_the_refresh_token_once_and_caches_the_result()
    {
        var time = new ManualTimeProvider(Start);
        var handler = new FakeHttpMessageHandler().Reply(StravaReply("access-1", Start.AddHours(6)));
        var provider = Strava(handler, time);

        Assert.Equal("access-1", await provider.GetAccessTokenAsync());
        time.Advance(TimeSpan.FromHours(5));
        Assert.Equal("access-1", await provider.GetAccessTokenAsync());

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://www.strava.com/oauth/token", request.RequestUri!.ToString());
        Assert.Contains("grant_type=refresh_token", handler.RequestBodies[0]);
        Assert.Contains("refresh_token=refresh-0", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task Strava_refreshes_again_once_the_token_is_about_to_expire_using_the_rotated_refresh_token()
    {
        var time = new ManualTimeProvider(Start);
        var handler = new FakeHttpMessageHandler()
            .Reply(StravaReply("access-1", Start.AddHours(6), refresh: "refresh-1"))
            .Reply(StravaReply("access-2", Start.AddHours(12), refresh: "refresh-2"));
        var provider = Strava(handler, time);

        await provider.GetAccessTokenAsync();
        time.Advance(TimeSpan.FromHours(6).Subtract(TimeSpan.FromSeconds(30)));

        // Inside the 60 second safety margin, so the cached token is no longer trusted.
        Assert.Equal("access-2", await provider.GetAccessTokenAsync());
        Assert.Contains("refresh_token=refresh-1", handler.RequestBodies[1]);
    }

    [Fact]
    public async Task Spotify_caches_for_the_lifetime_it_reports_then_refreshes()
    {
        var time = new ManualTimeProvider(Start);
        var handler = new FakeHttpMessageHandler()
            .Reply(SpotifyReply("access-1", expiresIn: 3600))
            .Reply(SpotifyReply("access-2", refresh: "refresh-new"));
        var provider = Spotify(handler, time);

        Assert.Equal("access-1", await provider.GetAccessTokenAsync());
        time.Advance(TimeSpan.FromMinutes(30));
        Assert.Equal("access-1", await provider.GetAccessTokenAsync());
        Assert.Single(handler.Requests);

        time.Advance(TimeSpan.FromMinutes(30));
        Assert.Equal("access-2", await provider.GetAccessTokenAsync());
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Spotify_authenticates_with_basic_credentials_and_keeps_the_old_refresh_token_when_none_is_returned()
    {
        var time = new ManualTimeProvider(Start);
        var handler = new FakeHttpMessageHandler()
            .Reply(SpotifyReply("access-1", expiresIn: 60))
            .Reply(SpotifyReply("access-2"));
        var provider = Spotify(handler, time);

        await provider.GetAccessTokenAsync();
        time.Advance(TimeSpan.FromMinutes(5));
        await provider.GetAccessTokenAsync();

        Assert.Equal("Basic", handler.Requests[0].Headers.Authorization!.Scheme);
        Assert.Equal("https://accounts.spotify.com/api/token", handler.Requests[0].RequestUri!.ToString());
        Assert.Contains("refresh_token=refresh-0", handler.RequestBodies[1]);
    }

    [Fact]
    public async Task A_failed_exchange_is_an_upstream_failure_that_does_not_leak_the_credentials()
    {
        var handler = new FakeHttpMessageHandler().Reply("""{"message":"Bad Request"}""", HttpStatusCode.BadRequest);
        var provider = Strava(handler, new ManualTimeProvider(Start));

        var error = await Assert.ThrowsAsync<UpstreamServiceException>(() => provider.GetAccessTokenAsync());

        Assert.DoesNotContain("shh", error.Message);
        Assert.DoesNotContain("refresh-0", error.Message);
    }
}
