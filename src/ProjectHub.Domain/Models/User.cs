using System.Net.Mail;
using ProjectHub.Domain.Exceptions;

namespace ProjectHub.Domain.Models;

/// <summary>Represents an account member and the authentication identity associated with it.</summary>
public sealed class User
{
    private User() { }

    private User(
        Guid accountId,
        string firstName,
        string lastName,
        string email,
        UserRole role,
        string? passwordHash,
        UserStatus status
    )
    {
        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("An account is required.");
        }

        Id = Guid.NewGuid();
        AccountId = accountId;
        FirstName = ValidateName(firstName);
        LastName = ValidateName(lastName);
        (Email, NormalizedEmail) = ValidateEmail(email);
        Role = ValidateRole(role);
        PasswordHash = passwordHash is null ? null : ValidatePasswordHash(passwordHash);
        Status = status;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the user identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the tenant account identifier.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>Gets the user's first name.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Gets the user's last name.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Gets the user's email address.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the normalized email used for unique lookup and authentication.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    /// <summary>Gets the account-scoped role.</summary>
    public UserRole Role { get; private set; }

    /// <summary>Gets the lifecycle state.</summary>
    public UserStatus Status { get; private set; }

    /// <summary>Gets the non-reversible password hash, or null until invitation acceptance.</summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Gets when the user was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets when the user was last updated, if applicable.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Gets when the user was deactivated, if applicable.</summary>
    public DateTimeOffset? DeactivatedAt { get; private set; }

    /// <summary>Creates the initial active account owner.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="passwordHash">A securely computed password hash.</param>
    /// <returns>The active owner.</returns>
    public static User CreateOwner(
        Guid accountId,
        string firstName,
        string lastName,
        string email,
        string passwordHash
    ) =>
        new(accountId, firstName, lastName, email, UserRole.Admin, passwordHash, UserStatus.Active);

    /// <summary>Creates a pending account member that must accept an invitation.</summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="role">The account-scoped role to assign.</param>
    /// <returns>The pending user.</returns>
    public static User Invite(
        Guid accountId,
        string firstName,
        string lastName,
        string email,
        UserRole role
    ) => new(accountId, firstName, lastName, email, role, null, UserStatus.Pending);

    /// <summary>Sets the password and activates a pending user.</summary>
    /// <param name="passwordHash">A securely computed password hash.</param>
    public void Activate(string passwordHash)
    {
        if (Status != UserStatus.Pending)
        {
            throw new DomainConflictException("Only a pending user can be activated.");
        }

        PasswordHash = ValidatePasswordHash(passwordHash);
        Status = UserStatus.Active;
        Touch();
    }

    /// <summary>Changes the password hash for an active user.</summary>
    /// <param name="passwordHash">A securely computed password hash.</param>
    public void ChangePasswordHash(string passwordHash)
    {
        EnsureActive();
        PasswordHash = ValidatePasswordHash(passwordHash);
        Touch();
    }

    /// <summary>Changes this user's account-scoped role.</summary>
    /// <param name="role">The new role.</param>
    public void ChangeRole(UserRole role)
    {
        EnsureActive();
        Role = ValidateRole(role);
        Touch();
    }

    /// <summary>Updates this user's name.</summary>
    /// <param name="firstName">The new first name.</param>
    /// <param name="lastName">The new last name.</param>
    public void UpdateName(string firstName, string lastName)
    {
        EnsureActive();
        FirstName = ValidateName(firstName);
        LastName = ValidateName(lastName);
        Touch();
    }

    /// <summary>Disables this user.</summary>
    public void Disable()
    {
        EnsureActive();
        var now = DateTimeOffset.UtcNow;
        Status = UserStatus.Disabled;
        DeactivatedAt = now;
        UpdatedAt = now;
    }

    private void EnsureActive()
    {
        if (Status != UserStatus.Active || DeactivatedAt is not null)
        {
            throw new DomainConflictException("An inactive user cannot be changed.");
        }
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string ValidateName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("A name is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > 100)
        {
            throw new DomainValidationException("A name cannot exceed 100 characters.");
        }

        return normalized;
    }

    private static (string Email, string NormalizedEmail) ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainValidationException("The email address is required.");
        }

        var candidate = email.Trim();
        try
        {
            var address = new MailAddress(candidate);
            if (!string.Equals(address.Address, candidate, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }

            if (address.Address.Length > 320)
            {
                throw new DomainValidationException(
                    "The email address cannot exceed 320 characters."
                );
            }

            return (address.Address, address.Address.ToUpperInvariant());
        }
        catch (FormatException)
        {
            throw new DomainValidationException("The email address is invalid.");
        }
    }

    private static string ValidatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainValidationException("A password hash is required.");
        }

        return passwordHash;
    }

    private static UserRole ValidateRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainValidationException("The user role is invalid.");
        }

        return role;
    }
}
