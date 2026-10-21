using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProjectHub.Application.Contracts;

namespace ProjectHub.Api.Security;

/// <summary>Creates signed JWT access tokens for authenticated users.</summary>
public sealed class JwtAccessTokenIssuer(JwtOptions options) : IAccessTokenIssuer
{
    /// <inheritdoc />
    public AccessToken Issue(Guid userId, Guid accountId)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(options.LifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString("D")),
            new Claim("account_id", accountId.ToString("D")),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256
        );
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials
        );

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
