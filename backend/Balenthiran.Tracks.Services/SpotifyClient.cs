using System.Net.Http.Headers;
using System.Text.Json;
using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;

namespace Balenthiran.Tracks.Services;

public class SpotifyClient(HttpClient http, ISpotifyTokenProvider tokens) : ISpotifyClient
{
    public async Task<IReadOnlyList<ISpotifyPlay>> GetRecentlyPlayedAsync(Guid userId, DateTimeOffset after, CancellationToken cancellationToken = default)
    {
        var token = await tokens.GetAccessTokenAsync(userId, cancellationToken);
        var url = $"https://api.spotify.com/v1/me/player/recently-played?limit=50&after={after.ToUnixTimeMilliseconds()}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url)
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
            throw new UpstreamServiceException("Spotify was unreachable.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new UpstreamServiceException($"Spotify recently-played request failed with status {(int)response.StatusCode}.");

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var plays = new List<ISpotifyPlay>();
            foreach (var item in body.RootElement.GetProperty("items").EnumerateArray())
            {
                var track = item.GetProperty("track");
                plays.Add(new SpotifyPlay
                {
                    TrackId = track.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                    TrackName = track.GetProperty("name").GetString() ?? string.Empty,
                    Artists = track.GetProperty("artists").EnumerateArray()
                        .Select(a => a.GetProperty("name").GetString() ?? string.Empty)
                        .ToList(),
                    AlbumArtUrl = FirstImageUrl(track),
                    DurationMs = track.GetProperty("duration_ms").GetInt32(),
                    PlayedAt = item.GetProperty("played_at").GetDateTimeOffset(),
                    ContextType = Context(item, "type"),
                    ContextUri = Context(item, "uri"),
                });
            }

            return plays;
        }
    }

    public async Task<string?> GetContextNameAsync(Guid userId, string contextType, string contextUri, CancellationToken cancellationToken = default)
    {
        // Only these three have a name endpoint; the id is the last segment of spotify:<type>:<id>.
        var id = contextUri.Split(':')[^1];
        var url = contextType switch
        {
            "album" => $"https://api.spotify.com/v1/albums/{id}",
            "playlist" => $"https://api.spotify.com/v1/playlists/{id}?fields=name",
            "artist" => $"https://api.spotify.com/v1/artists/{id}",
            _ => null,
        };
        if (url is null || id.Length == 0)
            return null;

        var token = await tokens.GetAccessTokenAsync(userId, cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, url)
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
            throw new UpstreamServiceException("Spotify was unreachable.", ex);
        }

        // A private or Spotify-editorial playlist answers 403/404; the caller keeps the type without a name.
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return body.RootElement.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() : null;
        }
    }

    // `context` is null (or absent) for plays that came from a queue.
    private static string? Context(JsonElement item, string key) =>
        item.TryGetProperty("context", out var context)
        && context.ValueKind == JsonValueKind.Object
        && context.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? FirstImageUrl(JsonElement track)
    {
        if (!track.TryGetProperty("album", out var album) || !album.TryGetProperty("images", out var images))
            return null;
        foreach (var image in images.EnumerateArray())
            return image.GetProperty("url").GetString();
        return null;
    }
}
