using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Balenthiran.Tracks.Tests.Sync;

/// <summary>
/// The real host with Strava configured and the processor replaced, so the webhook -> queue ->
/// worker path runs for real but nothing leaves the process.
/// </summary>
public sealed class SyncApiFactory : WebApplicationFactory<Program>
{
    public const long AthleteId = 4242;
    public const string VerifyToken = "verify-me";

    public RecordingProcessor Processor { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStravaSettings>();
            services.AddSingleton<IStravaSettings>(new StravaSettings { VerifyToken = VerifyToken, AthleteId = AthleteId });
            services.RemoveAll<IRunSyncProcessor>();
            services.AddSingleton<IRunSyncProcessor>(Processor);
        });
    }
}
