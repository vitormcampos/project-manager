using ProjectHub.Domain.Exceptions;

namespace ProjectHub.Domain.Models;

/// <summary>Represents a tenant or trusted platform-administration account.</summary>
public sealed class Account
{
    private Account() { }

    private Account(string name, AccountRole role)
    {
        Id = Guid.NewGuid();
        Name = ValidateName(name);
        Role = ValidateRole(role);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the account identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the account display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the system-wide account classification.</summary>
    public AccountRole Role { get; private set; }

    /// <summary>Gets when the account was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets when the account was last updated, if applicable.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>Gets when the account was deactivated, if applicable.</summary>
    public DateTimeOffset? DeactivatedAt { get; private set; }

    /// <summary>Creates a conventional tenant account.</summary>
    /// <param name="name">The account display name.</param>
    /// <returns>The new conventional account.</returns>
    public static Account CreateTenant(string name) => new(name, AccountRole.User);

    /// <summary>
    /// Creates a platform-administration account. Callers must restrict this operation to trusted provisioning.
    /// Every user belonging to this account receives system-wide authorization.
    /// </summary>
    /// <param name="name">The account display name.</param>
    /// <returns>The new platform-administration account.</returns>
    public static Account CreatePlatformAccount(string name) => new(name, AccountRole.Admin);

    /// <summary>Updates the account name.</summary>
    /// <param name="name">The new display name.</param>
    public void Rename(string name)
    {
        EnsureActive();
        Name = ValidateName(name);
        Touch();
    }

    /// <summary>Records a persisted membership change for concurrency control.</summary>
    public void RecordMembershipChange()
    {
        EnsureActive();
        Touch();
    }

    /// <summary>Deactivates this account.</summary>
    public void Deactivate()
    {
        EnsureActive();
        var now = DateTimeOffset.UtcNow;
        DeactivatedAt = now;
        UpdatedAt = now;
    }

    private void EnsureActive()
    {
        if (DeactivatedAt is not null)
        {
            throw new DomainConflictException("A deactivated account cannot be changed.");
        }
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("The account name is required.");
        }

        var normalized = name.Trim();
        if (normalized.Length > 200)
        {
            throw new DomainValidationException("The account name cannot exceed 200 characters.");
        }

        return normalized;
    }

    private static AccountRole ValidateRole(AccountRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainValidationException("The account role is invalid.");
        }

        return role;
    }
}
