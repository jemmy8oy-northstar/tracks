using System.Security.Claims;

namespace Balenthiran.Tracks.WebApi;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's id. Only valid on routes that require authorisation.</summary>
    public static Guid UserId(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
