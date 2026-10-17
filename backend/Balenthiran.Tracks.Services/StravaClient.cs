using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

public class StravaClient(HttpClient http, IStravaTokenProvider tokens) : IStravaClient
{
    public async Task<IStravaActivity> GetActivityAsync(long activityId, CancellationToken cancellationToken = default)
    {
        var token = await tokens.GetAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://www.strava.com/api/v3/activities/{activityId}")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
        };

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new UpstreamServiceException("Strava was unreachable.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new NotFoundException($"Strava activity {activityId} was not found.");
            if (!response.IsSuccessStatusCode)
                throw new UpstreamServiceException($"Strava activity request failed with status {(int)response.StatusCode}.");

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = body.RootElement;
            return new StravaActivity
            {
                Id = activityId,
                Name = root.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                // sport_type supersedes the older type field, which older activities may still only carry.
                SportType = (root.TryGetProperty("sport_type", out var sport) ? sport.GetString()
                    : root.TryGetProperty("type", out var type) ? type.GetString() : null) ?? string.Empty,
                StartDate = root.GetProperty("start_date").GetDateTimeOffset(),
                ElapsedTimeSeconds = root.GetProperty("elapsed_time").GetInt32(),
            };
        }
    }
}
