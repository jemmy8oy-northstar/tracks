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

    /// <summary>A reply that is bytes, not text (a downloaded file).</summary>
    public FakeHttpMessageHandler ReplyBytes(byte[] body, HttpStatusCode status = HttpStatusCode.OK)
    {
        binaryReplies[replies.Count] = body;
        replies.Enqueue((status, string.Empty));
        return this;
    }

    private readonly Dictionary<int, byte[]> binaryReplies = [];
    private int served;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        var (status, body) = replies.Dequeue();
        HttpContent content = binaryReplies.TryGetValue(served++, out var bytes) ? new ByteArrayContent(bytes) : new StringContent(body, Encoding.UTF8, "application/json");
        return new HttpResponseMessage(status) { Content = content };
    }
}
