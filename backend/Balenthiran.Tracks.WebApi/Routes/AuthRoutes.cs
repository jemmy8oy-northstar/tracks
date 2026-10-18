using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Balenthiran.Tracks.WebApi.Routes;

/// <summary>
/// Sign in with Strava (the account), connect Spotify to it, and read who is signed in.
/// Both OAuth round trips carry a random <c>state</c> in a short-lived cookie so a callback can only
/// complete in the browser that started it.
/// </summary>
public static class AuthRoutes
{
    private const string StravaState = "tracks.state.strava";
    private const string SpotifyState = "tracks.state.spotify";

    public static RouteGroupBuilder MapAuthRoutes(this RouteGroupBuilder parentGroup)
    {
        var auth = parentGroup.MapGroup("/auth");

        auth.MapGet("/strava", StartStravaSignIn).WithName("StartStravaSignIn");
        auth.MapGet("/strava/callback", FinishStravaSignIn).WithName("FinishStravaSignIn");
        // The cast matters: a lone-HttpContext handler otherwise binds to the RequestDelegate
        // overload, which carries no endpoint metadata, so the route drops out of openapi.json.
        auth.MapPost("/signout", (Delegate)SignOutUser).WithName("SignOut");

        auth.MapGet("/spotify", StartSpotifyConnect).RequireAuthorization().WithName("StartSpotifyConnect");
        auth.MapGet("/spotify/callback", FinishSpotifyConnect).RequireAuthorization().WithName("FinishSpotifyConnect");
        auth.MapDelete("/spotify", DisconnectSpotify).RequireAuthorization().WithName("DisconnectSpotify");

        parentGroup.MapGet("/me", GetMe).RequireAuthorization().WithName("GetMe");

        return parentGroup;
    }

    private static RedirectHttpResult StartStravaSignIn(HttpContext http, IStravaAuthService strava, IConfiguration config) =>
        TypedResults.Redirect(strava.BuildAuthorizeUrl(CallbackUrl(http, config, "strava"), NewState(http, StravaState)));

    private static async Task<RedirectHttpResult> FinishStravaSignIn(
        HttpContext http,
        IStravaAuthService strava,
        IConfiguration config,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? scope,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        CheckState(http, StravaState, state);
        if (error is not null || string.IsNullOrEmpty(code))
            throw new ValidationException("Strava did not grant access.");

        var user = await strava.SignInAsync(code, scope, cancellationToken);
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });

        return TypedResults.Redirect(BaseUrl(http, config) + "/");
    }

    private static async Task<Ok> SignOutUser(HttpContext http)
    {
        await http.SignOutAsync();
        return TypedResults.Ok();
    }

    private static RedirectHttpResult StartSpotifyConnect(HttpContext http, ISpotifyAuthService spotify, IConfiguration config) =>
        TypedResults.Redirect(spotify.BuildAuthorizeUrl(CallbackUrl(http, config, "spotify"), NewState(http, SpotifyState)));

    private static async Task<RedirectHttpResult> FinishSpotifyConnect(
        HttpContext http,
        ClaimsPrincipal principal,
        ISpotifyAuthService spotify,
        IConfiguration config,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken cancellationToken)
    {
        CheckState(http, SpotifyState, state);
        if (error is not null || string.IsNullOrEmpty(code))
            throw new ValidationException("Spotify did not grant access.");

        await spotify.ConnectAsync(principal.UserId(), code, CallbackUrl(http, config, "spotify"), cancellationToken);
        return TypedResults.Redirect(BaseUrl(http, config) + "/");
    }

    private static async Task<NoContent> DisconnectSpotify(ClaimsPrincipal principal, ISpotifyAuthService spotify, CancellationToken cancellationToken)
    {
        await spotify.DisconnectAsync(principal.UserId(), cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<MeResponse>> GetMe(ClaimsPrincipal principal, IUserService users, CancellationToken cancellationToken)
    {
        var user = await users.GetSignedInAsync(principal.UserId(), cancellationToken);
        return TypedResults.Ok(new MeResponse(user.StravaAthleteId, user.SpotifyConnected));
    }

    // The app's public root, which the OAuth providers send people back to. A configured PublicBaseUrl
    // wins (behind a TLS-terminating ingress the request alone says http, and Spotify needs an exact
    // match with the registered redirect URI); otherwise it is read from the request, PathBase included.
    private static string BaseUrl(HttpContext http, IConfiguration config) =>
        (config["PublicBaseUrl"] is { Length: > 0 } configured
            ? configured
            : $"{http.Request.Scheme}://{http.Request.Host}{http.Request.PathBase}").TrimEnd('/');

    private static string CallbackUrl(HttpContext http, IConfiguration config, string provider) =>
        $"{BaseUrl(http, config)}/api/auth/{provider}/callback";

    private static string NewState(HttpContext http, string cookie)
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        http.Response.Cookies.Append(cookie, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax, // Lax so the cookie rides along on the provider's redirect back.
            MaxAge = TimeSpan.FromMinutes(10),
            IsEssential = true,
        });
        return state;
    }

    private static void CheckState(HttpContext http, string cookie, string? state)
    {
        var expected = http.Request.Cookies[cookie];
        http.Response.Cookies.Delete(cookie);
        if (string.IsNullOrEmpty(expected) || state is null
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(state)))
            throw new ValidationException("The sign-in attempt could not be verified. Start again.");
    }
}
