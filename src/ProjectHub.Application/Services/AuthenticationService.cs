using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using ProjectHub.Application.Contracts;
using ProjectHub.Contracts;
using ProjectHub.Domain.Models;
using DomainUserStatus = ProjectHub.Domain.Models.UserStatus;

namespace ProjectHub.Application.Services;

/// <summary>Implements account registration and password authentication use cases.</summary>
public sealed class AuthenticationService(
    DbContext dbContext,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer
)
{
    /// <summary>Registers a conventional account and its initial owner atomically.</summary>
    /// <param name="request">The account and owner details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new owner's authentication result.</returns>
    public async Task<AuthenticationResult> RegisterAccountAsync(
        RegisterAccountRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidatePassword(request.Password);

        var account = Account.CreateTenant(request.AccountName);
        var passwordHash = passwordHasher.Hash(request.Password);
        var owner = User.CreateOwner(
            account.Id,
            request.FirstName,
            request.LastName,
            request.Email,
            passwordHash
        );

        if (
            await dbContext
                .Set<User>()
                .AnyAsync(user => user.NormalizedEmail == owner.NormalizedEmail, cancellationToken)
        )
        {
            throw new InvalidOperationException("The email is already registered.");
        }

        dbContext.Set<Account>().Add(account);
        dbContext.Set<User>().Add(owner);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (
                await dbContext
                    .Set<User>()
                    .AnyAsync(
                        user => user.NormalizedEmail == owner.NormalizedEmail,
                        cancellationToken
                    )
            )
            {
                throw new InvalidOperationException("The email is already registered.");
            }

            throw;
        }

        return CreateResult(owner);
    }

    /// <summary>Authenticates an active user with an email address and password.</summary>
    /// <param name="request">The submitted credentials.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authenticated user's access token and profile details.</returns>
    public async Task<AuthenticationResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            throw InvalidCredentials();
        }

        string normalizedEmail;
        try
        {
            var address = new MailAddress(request.Email.Trim());
            if (
                !string.Equals(
                    address.Address,
                    request.Email.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                throw new FormatException();
            }

            normalizedEmail = address.Address.ToUpperInvariant();
        }
        catch (FormatException)
        {
            throw InvalidCredentials();
        }

        var user = await dbContext
            .Set<User>()
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedEmail == normalizedEmail,
                cancellationToken
            );
        if (
            user?.PasswordHash is null
            || user.Status != DomainUserStatus.Active
            || user.DeactivatedAt is not null
            || !passwordHasher.Verify(user.PasswordHash, request.Password)
        )
        {
            throw InvalidCredentials();
        }

        var account = await dbContext
            .Set<Account>()
            .SingleOrDefaultAsync(candidate => candidate.Id == user.AccountId, cancellationToken);
        if (account is null || account.DeactivatedAt is not null)
        {
            throw InvalidCredentials();
        }

        return CreateResult(user);
    }

    private AuthenticationResult CreateResult(User user)
    {
        var token = tokenIssuer.Issue(user.Id, user.AccountId);
        return new AuthenticationResult(
            user.Id,
            user.AccountId,
            user.Email,
            user.FirstName,
            user.LastName,
            new ProjectHub.Contracts.AccessToken(token.Value, token.ExpiresAt)
        );
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 128)
        {
            throw new ArgumentException("The password must contain between 12 and 128 characters.");
        }
    }

    private static UnauthorizedAccessException InvalidCredentials() =>
        new("The email or password is invalid.");
}
