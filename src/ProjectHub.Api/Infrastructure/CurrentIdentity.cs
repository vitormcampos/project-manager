using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ProjectHub.Api.Infrastructure;

/// <summary>Reads the authenticated subject identifier from a validated bearer token.</summary>
public static class CurrentIdentity
{
    /// <summary>Gets the user identifier claim or rejects the request as unauthenticated.</summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The user identifier from the token subject.</returns>
    public static Guid GetUserId(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (Guid.TryParse(value, out var userId) && userId != Guid.Empty)
        {
            return userId;
        }

        throw new UnauthorizedAccessException("The access token is invalid.");
    }
}
