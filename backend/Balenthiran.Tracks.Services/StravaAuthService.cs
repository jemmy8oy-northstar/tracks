using System.Net;
using System.Text.Json;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.Database;
using Balenthiran.Tracks.EntityModels;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Services;

public class StravaAuthService(HttpClient http, AppDbContext db, IUserService users, IStravaSettings settings, TimeProvider time)
    : IStravaAuthService
{
    private const string RequiredScope = "activity:read_all";

    public string BuildAuthorizeUrl(string redirectUri, string state) =>
        QueryHelpers.AddQueryString("https://www.strava.com/oauth/authorize", new Dictionary<string, string?>
        {
            ["client_id"] = settings.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["approval_prompt"] = "auto",
            ["scope"] = RequiredScope,
            ["state"] = state,
        });

    public async Task<IUser> SignInAsync(string code, string? grantedScope, CancellationToken cancellationToken = default)
    {
        // The athlete can untick the activity box on Strava's page; without it every sync would fail later.
        if (grantedScope is null || !grantedScope.Split(',').Contains(RequiredScope))
            throw new ValidationException($"Strava access to your activities ({RequiredScope}) is needed to find your runs.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.strava.com/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = settings.ClientId ?? string.Empty,
                ["client_secret"] = settings.ClientSecret ?? string.Empty,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
            }),
        };

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new UpstreamServiceException("Strava token endpoint was unreachable.", ex);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
                throw new UnauthorizedException("Strava did not accept that sign-in. Try again.");
            // Strava's own cap on connected athletes (we keep no count of our own): a 403 naming it.
            if (response.StatusCode is HttpStatusCode.Forbidden
                && (await response.Content.ReadAsStringAsync(cancellationToken)).Contains("Limit of connected athletes exceeded", StringComparison.OrdinalIgnoreCase))
                throw new StravaFullException("Strava has reached its limit of connected athletes.");
            if (!response.IsSuccessStatusCode)
                throw new UpstreamServiceException($"Strava token exchange failed with status {(int)response.StatusCode}.");

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var athleteId = body.RootElement.GetProperty("athlete").GetProperty("id").GetInt64();
            var refreshToken = body.RootElement.GetProperty("refresh_token").GetString()!;

            var user = await db.Users.FirstOrDefaultAsync(u => u.StravaAthleteId == athleteId, cancellationToken);
            if (user is null)
                db.Users.Add(new UserEntity { Id = Guid.NewGuid(), StravaAthleteId = athleteId, StravaRefreshToken = refreshToken, CreatedAt = time.GetUtcNow() });
            else
                user.StravaRefreshToken = refreshToken;
            await db.SaveChangesAsync(cancellationToken);

            return (await users.FindByAthleteAsync(athleteId, cancellationToken))!;
        }
    }
}
