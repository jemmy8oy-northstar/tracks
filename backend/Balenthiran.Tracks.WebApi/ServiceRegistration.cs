using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Balenthiran.Tracks.Services;
using Balenthiran.Tracks.Database;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Balenthiran.Tracks.WebApi;

public static class ServiceRegistration
{
    public static void AddBackendServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("[WARNING] No database connection string configured — database features are disabled.");
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Balenthiran.Tracks.Database")));

            // Persist keys so auth cookies survive redeploys. Only with a database: without one the
            // context isn't registered and the default (ephemeral) key ring is used.
            services.AddDataProtection()
                .PersistKeysToDbContext<AppDbContext>()
                .SetApplicationName("tracks");
        }

        services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));
        services.AddScoped<IStatusService, StatusService>();

        // Strava -> Spotify run sync. The queue, token caches and clients are singletons (they must
        // outlive a request, and the worker runs outside any); what touches the database is scoped.
        // Settings are read when first resolved (not here) so test and host overrides are seen.
        services.AddSingleton<IStravaSettings>(sp =>
            sp.GetRequiredService<IConfiguration>().GetSection("Strava").Get<StravaSettings>() ?? new StravaSettings());
        services.AddSingleton<ISpotifySettings>(sp =>
            sp.GetRequiredService<IConfiguration>().GetSection("Spotify").Get<SpotifySettings>() ?? new SpotifySettings());
        services.AddSingleton(TimeProvider.System);
        services.AddHttpClient();
        services.AddScoped<IUserService, UserService>();
        services.AddHttpClient<IStravaAuthService, StravaAuthService>();
        services.AddHttpClient<ISpotifyAuthService, SpotifyAuthService>();
        services.AddSingleton<IStravaTokenProvider>(sp => new StravaTokenProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(), sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IStravaSettings>(), sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<ISpotifyTokenProvider>(sp => new SpotifyTokenProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(), sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<ISpotifySettings>(), sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IStravaClient>(sp => new StravaClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(), sp.GetRequiredService<IStravaTokenProvider>()));
        services.AddSingleton<ISpotifyClient>(sp => new SpotifyClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(), sp.GetRequiredService<ISpotifyTokenProvider>()));
        services.AddSingleton<IRunSongMatcher, RunSongMatcher>();
        services.AddScoped<IRunStore, RunStore>();
        services.AddSingleton<IRunSyncQueue, RunSyncQueue>();
        services.AddScoped<IRunSyncProcessor, RunSyncProcessor>();
        services.AddScoped<IStravaWebhookService, StravaWebhookService>();
        services.AddHostedService<RunSyncWorker>();
        services.AddSingleton<IStravaSubscriptionService>(sp => new StravaSubscriptionService(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(), sp.GetRequiredService<IStravaSettings>(),
            sp.GetRequiredService<IConfiguration>()["PublicBaseUrl"], sp.GetRequiredService<ILogger<StravaSubscriptionService>>()));
        services.AddHostedService(sp => new StravaSubscriptionWorker(
            sp.GetRequiredService<IStravaSubscriptionService>(), sp.GetRequiredService<IHostApplicationLifetime>(),
            sp.GetRequiredService<ILogger<StravaSubscriptionWorker>>(), StravaSubscriptionWorker.DefaultRetryDelays));
    }
}
