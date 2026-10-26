using System.Net;
using System.Net.Http.Json;
using Balenthiran.Tracks.Database;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>Sign in with Strava, connect Spotify, and who-am-I, against the real host and an in-memory database.</summary>
public class AuthApiTests
{
    private const long Athlete = 777;

    private sealed record Me(long AthleteId, bool SpotifyConnected);

    [Fact]
    public async Task Starting_sign_in_redirects_to_Strava_asking_for_activities_and_sets_a_state_cookie()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();

        var response = await browser.GetAsync("/api/auth/strava");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal("https://www.strava.com/oauth/authorize", location.GetLeftPart(UriPartial.Path));
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("strava-id", query["client_id"]);
        Assert.Equal("activity:read_all", query["scope"]);
        Assert.Equal("auto", query["approval_prompt"]);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("https://localhost/api/auth/strava/callback", query["redirect_uri"]);
        Assert.NotEmpty(query["state"].ToString());
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("tracks.state.strava="));
        Assert.Contains($"tracks.state.strava={query["state"]}", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_configured_PublicBaseUrl_is_what_the_redirect_uri_is_built_from()
    {
        using var factory = new SyncApiFactory(new Dictionary<string, string> { ["PublicBaseUrl"] = "https://balenthiran.co.uk/tracks/" });

        var response = await factory.Browser().GetAsync("/api/auth/strava");

        Assert.Equal(
            "https://balenthiran.co.uk/tracks/api/auth/strava/callback",
            QueryHelpers.ParseQuery(response.Headers.Location!.Query)["redirect_uri"]);
    }

    [Fact]
    public async Task Without_PublicBaseUrl_the_redirect_uri_follows_the_request_and_its_PathBase()
    {
        using var factory = new SyncApiFactory(new Dictionary<string, string> { ["PathBase"] = "/tracks" });

        var response = await factory.Browser().GetAsync("/tracks/api/auth/strava");

        Assert.Equal(
            "https://localhost/tracks/api/auth/strava/callback",
            QueryHelpers.ParseQuery(response.Headers.Location!.Query)["redirect_uri"]);
    }

    [Fact]
    public async Task A_callback_whose_state_does_not_match_is_rejected_and_creates_nobody()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.StartStravaAsync(browser);
        factory.Http.Reply("""{"refresh_token":"r","athlete":{"id":1}}""");

        var response = await browser.GetAsync($"/api/auth/strava/callback?code=c&state=forged&scope={SignIn.GrantedScope}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Http.Requests);
        Assert.Equal(0, await factory.WithDb(db => db.Users.CountAsync()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task A_callback_with_no_state_cookie_is_rejected()
    {
        using var factory = new SyncApiFactory();

        var response = await factory.Browser().GetAsync($"/api/auth/strava/callback?code=c&state=anything&scope={SignIn.GrantedScope}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_state_can_only_be_used_once()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        var state = await SignIn.StartStravaAsync(browser);
        factory.Http.Reply("""{"refresh_token":"r","athlete":{"id":1}}""");

        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync($"/api/auth/strava/callback?code=c&state={state}&scope={SignIn.GrantedScope}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.GetAsync($"/api/auth/strava/callback?code=c&state={state}&scope={SignIn.GrantedScope}")).StatusCode);
    }

    [Fact]
    public async Task A_grant_without_activity_access_is_refused()
    {
        using var factory = new SyncApiFactory();

        var response = await SignIn.WithStravaAsync(factory, factory.Browser(), Athlete, scope: "read");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await factory.WithDb(db => db.Users.CountAsync()));
    }

    [Fact]
    public async Task A_good_callback_creates_the_user_signs_them_in_and_goes_back_to_the_app()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();

        var response = await SignIn.WithStravaAsync(factory, browser, Athlete, refreshToken: "refresh-0");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://localhost/", response.Headers.Location!.ToString());
        var session = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("tracks.session="));
        Assert.Contains("httponly", session, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", session, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", session, StringComparison.OrdinalIgnoreCase);

        var exchange = factory.Http.RequestBodies.Single();
        Assert.Contains("code=the-code", exchange);
        Assert.Contains("client_secret=strava-secret", exchange);
        Assert.Contains("grant_type=authorization_code", exchange);
        var user = await factory.WithDb(db => db.Users.SingleAsync());
        Assert.Equal(Athlete, user.StravaAthleteId);
        Assert.Equal("refresh-0", user.StravaRefreshToken);
        Assert.Null(user.SpotifyRefreshToken);

        var me = await browser.GetFromJsonAsync<Me>("/api/me", new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal(new Me(Athlete, false), me);
    }

    [Fact]
    public async Task Signing_in_again_reuses_the_user_and_takes_the_new_refresh_token()
    {
        using var factory = new SyncApiFactory();

        await SignIn.WithStravaAsync(factory, factory.Browser(), Athlete, refreshToken: "old");
        await SignIn.WithStravaAsync(factory, factory.Browser(), Athlete, refreshToken: "new");

        var user = await factory.WithDb(db => db.Users.SingleAsync());
        Assert.Equal("new", user.StravaRefreshToken);
    }

    [Fact]
    public async Task A_rejected_code_is_a_401_not_a_server_error()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        var state = await SignIn.StartStravaAsync(browser);
        factory.Http.Reply("""{"message":"Bad Request"}""", HttpStatusCode.BadRequest);

        var response = await browser.GetAsync($"/api/auth/strava/callback?code=old&state={state}&scope={SignIn.GrantedScope}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Strava_refusing_a_new_athlete_for_its_cap_sends_them_home_as_full_and_creates_nobody_BEH_CONNECT_4()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        var state = await SignIn.StartStravaAsync(browser);
        factory.Http.Reply("""{"message":"Forbidden","errors":[{"resource":"Application","field":"athlete","code":"Limit of connected athletes exceeded"}]}""", HttpStatusCode.Forbidden);

        var response = await browser.GetAsync($"/api/auth/strava/callback?code=c&state={state}&scope={SignIn.GrantedScope}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://localhost/?error=full", response.Headers.Location!.ToString());
        Assert.DoesNotContain(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("tracks.session="));
        Assert.Equal(0, await factory.WithDb(db => db.Users.CountAsync()));
    }

    [Fact]
    public async Task A_403_from_Strava_for_any_other_reason_is_not_reported_as_full_BEH_CONNECT_4()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        var state = await SignIn.StartStravaAsync(browser);
        factory.Http.Reply("""{"message":"Forbidden"}""", HttpStatusCode.Forbidden);

        var response = await browser.GetAsync($"/api/auth/strava/callback?code=c&state={state}&scope={SignIn.GrantedScope}");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Me_is_401_when_signed_out()
    {
        using var factory = new SyncApiFactory();

        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Browser().GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Signing_out_ends_the_session()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/me")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await browser.PostAsync("/api/auth/signout", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task A_session_for_a_deleted_user_is_401()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);
        await factory.WithDb(async db =>
        {
            db.Users.RemoveRange(db.Users);
            return await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Connecting_Spotify_needs_a_session()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();

        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/spotify")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/spotify/callback?code=c&state=s")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.DeleteAsync("/api/auth/spotify")).StatusCode);
    }

    [Fact]
    public async Task Connecting_Spotify_stores_the_refresh_token_on_the_signed_in_user()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);

        var start = await browser.GetAsync("/api/auth/spotify");

        Assert.Equal("https://accounts.spotify.com/authorize", start.Headers.Location!.GetLeftPart(UriPartial.Path));
        var query = QueryHelpers.ParseQuery(start.Headers.Location.Query);
        Assert.Equal("user-read-recently-played", query["scope"]);
        Assert.Equal("spotify-id", query["client_id"]);
        Assert.Equal("https://localhost/api/auth/spotify/callback", query["redirect_uri"]);

        factory.Http.Reply("""{"access_token":"a","refresh_token":"spotify-refresh","expires_in":3600}""");
        var done = await browser.GetAsync($"/api/auth/spotify/callback?code=sp-code&state={query["state"]}");

        Assert.Equal(HttpStatusCode.Redirect, done.StatusCode);
        Assert.Equal("https://localhost/", done.Headers.Location!.ToString());
        var request = factory.Http.Requests.Last();
        Assert.Equal("https://accounts.spotify.com/api/token", request.RequestUri!.ToString());
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        var body = factory.Http.RequestBodies.Last();
        Assert.Contains("code=sp-code", body);
        Assert.Contains("redirect_uri=https%3A%2F%2Flocalhost%2Fapi%2Fauth%2Fspotify%2Fcallback", body);
        Assert.Equal("spotify-refresh", (await factory.WithDb(db => db.Users.SingleAsync())).SpotifyRefreshToken);
        Assert.Equal(new Me(Athlete, true), await browser.GetFromJsonAsync<Me>("/api/me", new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
    }

    [Fact]
    public async Task A_Spotify_callback_with_the_wrong_state_is_rejected()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);
        await browser.GetAsync("/api/auth/spotify");

        var response = await browser.GetAsync("/api/auth/spotify/callback?code=c&state=forged");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null((await factory.WithDb(db => db.Users.SingleAsync())).SpotifyRefreshToken);
    }

    [Fact]
    public async Task Disconnecting_Spotify_forgets_the_token()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);
        var state = QueryHelpers.ParseQuery((await browser.GetAsync("/api/auth/spotify")).Headers.Location!.Query)["state"];
        factory.Http.Reply("""{"access_token":"a","refresh_token":"spotify-refresh","expires_in":3600}""");
        await browser.GetAsync($"/api/auth/spotify/callback?code=c&state={state}");

        Assert.Equal(HttpStatusCode.NoContent, (await browser.DeleteAsync("/api/auth/spotify")).StatusCode);

        Assert.Null((await factory.WithDb(db => db.Users.SingleAsync())).SpotifyRefreshToken);
    }
}
