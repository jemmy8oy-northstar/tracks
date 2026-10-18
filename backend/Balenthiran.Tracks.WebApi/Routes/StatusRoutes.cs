using Balenthiran.Tracks.Abstractions.DataModels;
using Balenthiran.Tracks.Abstractions.DomainModels;
using Balenthiran.Tracks.Abstractions.Services;

namespace Balenthiran.Tracks.WebApi.Routes;

public static class StatusRoutes
{
    public static RouteGroupBuilder MapStatusRoutes(this RouteGroupBuilder parentGroup)
    {
        var group = parentGroup.MapGroup("/status");

        group.MapGet("", async (IStatusService statusService, IStravaSubscriptionService stravaSubscription) =>
        {
            var status = await statusService.GetSystemStatusAsync();
            return Results.Ok(new {
                version = status.Version,
                friendlyStatus = status.GetFriendlyStatus(),
                timestamp = status.LastUpdated,
                // Public so a live check can tell whether Strava will send runs at all; it names no secret.
                stravaWebhook = stravaSubscription.State
            });
        })
        .WithName("GetStatus");

        return parentGroup;
    }
}
