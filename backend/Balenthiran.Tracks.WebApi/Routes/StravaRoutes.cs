using Balenthiran.Tracks.Abstractions.Exceptions;
using Balenthiran.Tracks.Abstractions.Services;
using Balenthiran.Tracks.DataModels.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Balenthiran.Tracks.WebApi.Routes;

public static class StravaRoutes
{
    public static RouteGroupBuilder MapStravaRoutes(this RouteGroupBuilder parentGroup)
    {
        var group = parentGroup.MapGroup("/strava/webhook");

        group.MapGet("", VerifySubscription).WithName("VerifyStravaWebhook");
        group.MapPost("", ReceiveEvent).WithName("ReceiveStravaWebhookEvent");

        return parentGroup;
    }

    // Strava's subscription handshake: echo the challenge back only for our own verify token.
    private static Ok<Dictionary<string, string?>> VerifySubscription(
        IStravaWebhookService webhook,
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (!webhook.IsValidSubscription(mode, verifyToken))
            throw new ForbiddenException("The webhook verify token did not match.");

        return TypedResults.Ok(new Dictionary<string, string?> { ["hub.challenge"] = challenge });
    }

    // Acknowledge at once: Strava wants a reply within 2 seconds, so the real work happens on the queue.
    private static async Task<Ok> ReceiveEvent(IStravaWebhookService webhook, StravaWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        await webhook.AcceptAsync(webhookEvent, cancellationToken);
        return TypedResults.Ok();
    }
}
