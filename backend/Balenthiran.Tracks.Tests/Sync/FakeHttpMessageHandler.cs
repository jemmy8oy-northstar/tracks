using System.Net;
using System.Text;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>Replays canned replies in order and records what was sent, so no test touches the network.</summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> replies = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> RequestBodies { get; } = [];

    public FakeHttpMessageHandler Reply(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        replies.Enqueue((status, json));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        var (status, body) = replies.Dequeue();
        return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}
