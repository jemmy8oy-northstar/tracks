using Microsoft.AspNetCore.WebUtilities;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>Drives the real Strava sign-in round trip so a test browser ends up holding a genuine session cookie.</summary>
public static class SignIn
{
    public const string GrantedScope = "read,activity:read_all";

    /// <summary>What Strava reports when the runner left the activity:write box ticked too (BEH-SHARE-2).</summary>
    public const string GrantedWithWriteScope = "read,activity:read_all,activity:write";

    /// <summary>The <c>state</c> the authorize redirect carries (the browser also keeps it in a cookie).</summary>
    public static async Task<string> StartStravaAsync(HttpClient browser)
    {
        var start = await browser.GetAsync("/api/auth/strava");
        return QueryHelpers.ParseQuery(start.Headers.Location!.Query)["state"].ToString();
    }

    /// <summary>Signs the browser in as that Strava athlete; the fake token endpoint answers with <paramref name="refreshToken"/>.</summary>
    public static async Task<HttpResponseMessage> WithStravaAsync(
        SyncApiFactory factory, HttpClient browser, long athleteId, string refreshToken = "refresh-0", string scope = GrantedScope)
    {
        var state = await StartStravaAsync(browser);
        factory.Http.Reply($$$"""{"access_token":"a","refresh_token":"{{{refreshToken}}}","expires_at":1,"athlete":{"id":{{{athleteId}}}}}""");
        return await browser.GetAsync($"/api/auth/strava/callback?code=the-code&state={state}&scope={scope}");
    }
}
