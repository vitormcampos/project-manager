namespace ProjectHub.Application.Contracts;

/// <summary>Contains a signed access token and its expiration time for application use cases.</summary>
/// <param name="Value">The bearer token value.</param>
/// <param name="ExpiresAt">The token expiration timestamp.</param>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>Issues access tokens for authenticated users.</summary>
public interface IAccessTokenIssuer
{
    /// <summary>Creates a token identifying the user and their account.</summary>
    AccessToken Issue(Guid userId, Guid accountId);
}

/// <summary>Hashes passwords and verifies password candidates.</summary>
public interface IPasswordHasher
{
    /// <summary>Creates a secure, non-reversible password hash.</summary>
    string Hash(string password);

    /// <summary>Checks a password candidate against an existing hash.</summary>
    bool Verify(string passwordHash, string password);
}

/// <summary>Delivers an account invitation without persisting or logging its raw token.</summary>
public interface IInvitationDelivery
{
    /// <summary>Sends an invitation containing a one-time activation token.</summary>
    Task SendAsync(
        string email,
        string firstName,
        string rawToken,
        CancellationToken cancellationToken
    );
}
