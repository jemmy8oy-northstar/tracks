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
    public async Task<IStravaActivity> GetActivityAsync(Guid userId, long activityId, CancellationToken cancellationToken = default)
    {
        using (var response = await GetAsync(userId, $"https://www.strava.com/api/v3/activities/{activityId}", cancellationToken))
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

    public async Task<IActivityStreams?> GetStreamsAsync(Guid userId, long activityId, CancellationToken cancellationToken = default)
    {
        using var response = await GetAsync(
            userId,
            $"https://www.strava.com/api/v3/activities/{activityId}/streams?keys=time,distance,velocity_smooth,latlng&key_by_type=true",
            cancellationToken);

        // An activity with no GPS (a treadmill run, a manual entry) has no streams to give.
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            throw new UpstreamServiceException($"Strava streams request failed with status {(int)response.StatusCode}.");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = body.RootElement;
        var time = Stream(root, "time");
        var distance = Stream(root, "distance");
        if (time is null || distance is null)
            return null;

        return new ActivityStreams
        {
            TimeSeconds = time,
            DistanceMetres = distance,
            VelocityMetresPerSecond = Stream(root, "velocity_smooth") ?? [],
            LatLng = LatLngStream(root),
        };
    }

    // latlng is the one stream whose samples are pairs, [lat, lng]; anything else in it is dropped whole.
    private static IReadOnlyList<IReadOnlyList<double>> LatLngStream(JsonElement root) =>
        root.TryGetProperty("latlng", out var stream)
        && stream.ValueKind == JsonValueKind.Object
        && stream.TryGetProperty("data", out var data)
        && data.ValueKind == JsonValueKind.Array
        && data.EnumerateArray().All(p => p.ValueKind == JsonValueKind.Array && p.GetArrayLength() == 2)
            ? data.EnumerateArray().Select(p => (IReadOnlyList<double>)[p[0].GetDouble(), p[1].GetDouble()]).ToList()
            : [];

    private static List<double>? Stream(JsonElement root, string key) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty(key, out var stream)
        && stream.ValueKind == JsonValueKind.Object
        && stream.TryGetProperty("data", out var data)
        && data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray().Select(v => v.GetDouble()).ToList()
            : null;

    private async Task<HttpResponseMessage> GetAsync(Guid userId, string url, CancellationToken cancellationToken)
    {
        var token = await tokens.GetAccessTokenAsync(userId, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
        };

        try
        {
            return await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new UpstreamServiceException("Strava was unreachable.", ex);
        }
    }
}
