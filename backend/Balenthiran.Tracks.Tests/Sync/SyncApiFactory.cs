using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.Database;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>
/// The real host on an in-memory database, with Strava and Spotify configured, the processor replaced
/// and every outbound HTTP call answered by <see cref="Http"/>, so the webhook -> queue -> worker path
/// runs for real but nothing leaves the process.
/// </summary>
public sealed class SyncApiFactory(IReadOnlyDictionary<string, string>? settings = null) : WebApplicationFactory<Program>
{
    public const string VerifyToken = "verify-me";

    private readonly string store = Guid.NewGuid().ToString();

    public RecordingProcessor Processor { get; } = new();

    public FakeHttpMessageHandler Http { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
            builder.UseSetting(key, value);

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(store));
            services.RemoveAll<IStravaSettings>();
            services.AddSingleton<IStravaSettings>(new StravaSettings { ClientId = "strava-id", ClientSecret = "strava-secret", VerifyToken = VerifyToken });
            services.RemoveAll<ISpotifySettings>();
            services.AddSingleton<ISpotifySettings>(new SpotifySettings { ClientId = "spotify-id", ClientSecret = "spotify-secret" });
            services.RemoveAll<IRunSyncProcessor>();
            services.AddSingleton<IRunSyncProcessor>(Processor);
            services.ConfigureAll<HttpClientFactoryOptions>(o => o.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = Http));
        });
    }

    /// <summary>Runs <paramref name="act"/> against the factory's database in its own scope.</summary>
    public async Task<T> WithDb<T>(Func<AppDbContext, Task<T>> act)
    {
        using var scope = Services.CreateScope();
        return await act(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>A client that keeps cookies and follows no redirects: one browser.</summary>
    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        // https so the Secure cookies are sent back; no redirects so the tests can read them.
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
    });
}
