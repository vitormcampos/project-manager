using ProjectHub.Domain.Exceptions;

namespace ProjectHub.Domain.Models;

/// <summary>Represents a single-use invitation to activate a user account.</summary>
public sealed class UserInvitation
{
    private UserInvitation() { }

    private UserInvitation(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException("A user is required.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainValidationException("An invitation token hash is required.");
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new DomainValidationException("The invitation expiry must be in the future.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the invitation identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the invited user's identifier.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the hash of the one-time token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Gets the expiry time.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Gets when the invitation was accepted, if applicable.</summary>
    public DateTimeOffset? AcceptedAt { get; private set; }

    /// <summary>Gets when the invitation was revoked, if applicable.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Gets when the invitation was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates a new invitation record.</summary>
    /// <param name="userId">The invited user's identifier.</param>
    /// <param name="tokenHash">The one-way hash of the raw token.</param>
    /// <param name="expiresAt">The expiry time.</param>
    /// <returns>The new invitation.</returns>
    public static UserInvitation Create(Guid userId, string tokenHash, DateTimeOffset expiresAt) =>
        new(userId, tokenHash, expiresAt);

    /// <summary>Marks this invitation as accepted.</summary>
    public void Accept()
    {
        EnsureUsable();
        AcceptedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Revokes this invitation.</summary>
    public void Revoke()
    {
        EnsureUsable();
        RevokedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Indicates whether this invitation can still be accepted.</summary>
    public bool IsUsable(DateTimeOffset now) =>
        AcceptedAt is null && RevokedAt is null && ExpiresAt > now;

    private void EnsureUsable()
    {
        if (!IsUsable(DateTimeOffset.UtcNow))
        {
            throw new DomainConflictException("The invitation is no longer usable.");
        }
    }
}
