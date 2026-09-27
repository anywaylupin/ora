using System.Security.Claims;

namespace Ora.Api.Data;

/// <summary>
/// Reads the Identity user ID that the bearer token handler puts on the principal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
