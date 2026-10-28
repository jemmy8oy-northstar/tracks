using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>POST /api/runs/upload against the real host: the session, multipart binding, status codes, then the run in the list.</summary>
public class RunUploadApiTests
{
    private const long Athlete = 4242;

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private const string Plays = """
        {"items":[{"track":{"id":"t1","name":"Early","duration_ms":240000,"artists":[{"name":"A"}],"album":{"images":[]}},
                   "played_at":"2026-10-17T09:08:00.000Z"}]}
        """;

    private static MultipartFormDataContent Upload(byte[] file, string name = "ACTIVITY.fit")
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(file), "file", name);
        return form;
    }

    private static async Task ConnectSpotifyAsync(SyncApiFactory factory) =>
        await factory.WithDb(async db =>
        {
            (await db.Users.SingleAsync()).SpotifyRefreshToken = "spotify-0";
            return await db.SaveChangesAsync();
        });

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task Uploading_a_run_needs_a_session()
    {
        using var factory = new SyncApiFactory();

        var response = await factory.Browser().PostAsync("/api/runs/upload", Upload(FitFixture.Fit()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task An_uploaded_fit_shows_up_in_the_runs_with_its_songs_and_route()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);
        await ConnectSpotifyAsync(factory);
        factory.Http.Reply("""{"access_token":"s","expires_in":3600}""").Reply(Plays);

        var response = await browser.PostAsync("/api/runs/upload", Upload(FitFixture.Fit()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var runs = await browser.GetFromJsonAsync<List<RunResponse>>("/api/runs", Web);
        var run = Assert.Single(runs!);
        Assert.Equal(FitFixture.Start, run.StartedAt);
        Assert.Equal(["Early"], run.Songs.Select(s => s.Name));
        Assert.Equal(FitFixture.Records, run.Route.Count);
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task A_file_that_is_not_a_fit_is_refused_with_400()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);
        await ConnectSpotifyAsync(factory);

        var response = await browser.PostAsync("/api/runs/upload", Upload("hello"u8.ToArray(), "notes.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("[]", await browser.GetStringAsync("/api/runs"));
    }

    [Fact]
    [Trait("Behaviour", "BEH-SYNC-7")]
    public async Task A_request_with_no_file_is_refused_with_400()
    {
        using var factory = new SyncApiFactory();
        var browser = factory.Browser();
        await SignIn.WithStravaAsync(factory, browser, Athlete);

        var response = await browser.PostAsync("/api/runs/upload", new MultipartFormDataContent());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
