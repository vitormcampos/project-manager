namespace ProjectHub.Contracts;

/// <summary>Data required to register a conventional account and its owner.</summary>
public sealed record RegisterAccountRequest(
    string AccountName,
    string FirstName,
    string LastName,
    string Email,
    string Password
);

/// <summary>Credentials submitted to authenticate.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Information returned after successful authentication.</summary>
public sealed record AuthenticationResult(
    Guid UserId,
    Guid AccountId,
    string Email,
    string FirstName,
    string LastName,
    AccessToken AccessToken
);

/// <summary>Contains a signed access token and its expiration time.</summary>
/// <param name="Value">The bearer token value.</param>
/// <param name="ExpiresAt">The token expiration timestamp.</param>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
