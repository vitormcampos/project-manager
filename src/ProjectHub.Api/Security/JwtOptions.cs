namespace ProjectHub.Api.Security;

/// <summary>Configuration for signed bearer access tokens.</summary>
public sealed class JwtOptions
{
    /// <summary>Gets or sets the token issuer.</summary>
    public string Issuer { get; set; } = "ProjectHub";

    /// <summary>Gets or sets the token audience.</summary>
    public string Audience { get; set; } = "ProjectHub.Api";

    /// <summary>Gets or sets the private HMAC signing key from secret configuration.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the access-token lifetime in minutes.</summary>
    public int LifetimeMinutes { get; set; } = 15;

    /// <summary>Validates the security-sensitive token settings.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("JWT issuer and audience must be configured.");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 UTF-8 bytes."
            );
        }

        if (LifetimeMinutes is < 5 or > 1440)
        {
            throw new InvalidOperationException("JWT lifetime must be between 5 and 1440 minutes.");
        }
    }
}
