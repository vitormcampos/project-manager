using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ProjectHub.Application.Contracts;
using ProjectHub.Domain.Models;
using AccountSummary = ProjectHub.Contracts.AccountSummary;
using AuthenticationResult = ProjectHub.Contracts.AuthenticationResult;
using ContractAccountRole = ProjectHub.Contracts.AccountRole;
using ContractUserRole = ProjectHub.Contracts.UserRole;
using ContractUserStatus = ProjectHub.Contracts.UserStatus;
using InvitationCreated = ProjectHub.Contracts.InvitationCreated;
using UserSummary = ProjectHub.Contracts.UserSummary;

namespace ProjectHub.Application.Services;

/// <summary>Implements tenant-aware account and member management use cases.</summary>
public sealed class AccountUserService(
    DbContext dbContext,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer,
    IInvitationDelivery invitationDelivery
)
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromHours(24);

    /// <summary>Invites a user after checking the caller's persisted permissions.</summary>
    public async Task<InvitationCreated> InviteUserAsync(
        Guid actorId,
        Guid accountId,
        string firstName,
        string lastName,
        string email,
        UserRole role,
        CancellationToken cancellationToken = default
    )
    {
        var actor = await GetActiveActorAsync(actorId, cancellationToken);
        await EnsureCanManageAccountAsync(actor, accountId, cancellationToken);

        var user = User.Invite(accountId, firstName, lastName, email, role);

        if (
            await dbContext
                .Set<User>()
                .AnyAsync(
                    candidate => candidate.NormalizedEmail == user.NormalizedEmail,
                    cancellationToken
                )
        )
        {
            throw new InvalidOperationException("The email is already registered.");
        }

        var rawToken = CreateRawToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(InvitationLifetime);
        var invitation = UserInvitation.Create(user.Id, HashToken(rawToken), expiresAt);
        var transaction = await BeginTransactionIfRelationalAsync(cancellationToken);
        try
        {
            dbContext.Set<User>().Add(user);
            dbContext.Set<UserInvitation>().Add(invitation);
            await dbContext.SaveChangesAsync(cancellationToken);
            await invitationDelivery.SendAsync(
                user.Email,
                user.FirstName,
                rawToken,
                cancellationToken
            );
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                await transaction.DisposeAsync();
                transaction = null;
            }

            if (
                await dbContext
                    .Set<User>()
                    .AnyAsync(
                        candidate => candidate.NormalizedEmail == user.NormalizedEmail,
                        cancellationToken
                    )
            )
            {
                throw new InvalidOperationException("The email is already registered.");
            }

            throw;
        }
        catch (TimeoutException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw new TimeoutException("Invitation delivery failed.");
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        return new InvitationCreated(user.Id, expiresAt);
    }

    /// <summary>Revokes the outstanding invitation for a pending user.</summary>
    /// <param name="actorId">The authenticated caller's user identifier.</param>
    /// <param name="accountId">The account containing the invited user.</param>
    /// <param name="userId">The invited user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task RevokeInvitationAsync(
        Guid actorId,
        Guid accountId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var actor = await GetActiveActorAsync(actorId, cancellationToken);
        await EnsureCanManageAccountAsync(actor, accountId, cancellationToken);
        var user = await FindTargetUserAsync(accountId, userId, cancellationToken);
        if (user.Status != UserStatus.Pending)
        {
            throw new InvalidOperationException("Only pending users have active invitations.");
        }

        var now = DateTimeOffset.UtcNow;
        var invitations = await dbContext
            .Set<UserInvitation>()
            .Where(candidate =>
                candidate.UserId == userId
                && candidate.AcceptedAt == null
                && candidate.RevokedAt == null
            )
            .ToListAsync(cancellationToken);
        var invitation =
            invitations
                .Where(candidate => candidate.ExpiresAt > now)
                .OrderByDescending(candidate => candidate.CreatedAt)
                .FirstOrDefault()
            ?? throw new KeyNotFoundException("No active invitation was found.");

        invitation.Revoke();
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("The invitation has already been consumed.");
        }
    }

    /// <summary>Activates an invited user with a valid one-time token and a new password.</summary>
    public async Task<AuthenticationResult> AcceptInvitationAsync(
        string rawToken,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        ValidatePassword(password);
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 200)
        {
            throw InvalidInvitation();
        }

        var tokenHash = HashToken(rawToken);
        var invitation = await dbContext
            .Set<UserInvitation>()
            .SingleOrDefaultAsync(candidate => candidate.TokenHash == tokenHash, cancellationToken);
        if (invitation is null || !invitation.IsUsable(DateTimeOffset.UtcNow))
        {
            throw InvalidInvitation();
        }

        var user = await dbContext
            .Set<User>()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == invitation.UserId,
                cancellationToken
            );
        if (user is null || user.Status != UserStatus.Pending)
        {
            throw InvalidInvitation();
        }

        var account = await dbContext
            .Set<Account>()
            .SingleOrDefaultAsync(candidate => candidate.Id == user.AccountId, cancellationToken);
        if (account is null || account.DeactivatedAt is not null)
        {
            throw InvalidInvitation();
        }

        try
        {
            user.Activate(passwordHasher.Hash(password));
            invitation.Accept();
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw InvalidInvitation();
        }

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

    /// <summary>Lists users in an account for its administrator or a platform administrator.</summary>
    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(
        Guid actorId,
        Guid accountId,
        CancellationToken cancellationToken = default
    )
    {
        var actor = await GetActiveActorAsync(actorId, cancellationToken);
        await EnsureCanManageAccountAsync(actor, accountId, cancellationToken);

        var users = await dbContext
            .Set<User>()
            .Where(user => user.AccountId == accountId)
            .OrderBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.AccountId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.Status,
            })
            .ToListAsync(cancellationToken);

        return users
            .Select(user => new UserSummary(
                user.Id,
                user.AccountId,
                user.FirstName,
                user.LastName,
                user.Email,
                ToContractRole(user.Role),
                ToContractStatus(user.Status)
            ))
            .ToList();
    }

    /// <summary>Changes a member's account-scoped role.</summary>
    public async Task ChangeUserRoleAsync(
        Guid actorId,
        Guid accountId,
        Guid userId,
        UserRole role,
        CancellationToken cancellationToken = default
    )
    {
        var actor = await GetActiveActorAsync(actorId, cancellationToken);
        var targetAccount = await EnsureCanManageAccountAsync(actor, accountId, cancellationToken);
        var user = await FindTargetUserAsync(accountId, userId, cancellationToken);

        if (
            targetAccount.Role == AccountRole.User
            && user.Role == UserRole.Admin
            && role != UserRole.Admin
        )
        {
            await EnsureAnotherActiveAdministratorExistsAsync(accountId, userId, cancellationToken);
        }

        try
        {
            user.ChangeRole(role);
            targetAccount.RecordMembershipChange();
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw LastAdministratorConflict();
        }
    }

    /// <summary>Disables a member while preserving the last-admin invariant.</summary>
    public async Task DisableUserAsync(
        Guid actorId,
        Guid accountId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var actor = await GetActiveActorAsync(actorId, cancellationToken);
        var targetAccount = await EnsureCanManageAccountAsync(actor, accountId, cancellationToken);
        var user = await FindTargetUserAsync(accountId, userId, cancellationToken);

        if (targetAccount.Role == AccountRole.User && user.Role == UserRole.Admin)
        {
            await EnsureAnotherActiveAdministratorExistsAsync(accountId, userId, cancellationToken);
        }

        user.Disable();
        targetAccount.RecordMembershipChange();
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw LastAdministratorConflict();
        }
    }

    /// <summary>Returns all accounts to a platform administrator.</summary>
    public async Task<IReadOnlyList<AccountSummary>> GetAccountsAsync(
        Guid actorId,
        CancellationToken cancellationToken = default
    )
    {
        var actor = await GetActiveActorAsync(actorId, cancellationToken);
        EnsurePlatformAdministrator(actor);
        var accounts = await dbContext
            .Set<Account>()
            .OrderBy(account => account.Name)
            .Select(account => new
            {
                account.Id,
                account.Name,
                account.Role,
                account.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return accounts
            .Select(account => new AccountSummary(
                account.Id,
                account.Name,
                ToContractRole(account.Role),
                account.CreatedAt
            ))
            .ToList();
    }

    /// <summary>Deactivates an account. Only a platform administrator can call this operation.</summary>
    public async Task DeactivateAccountAsync(
        Guid actorId,
        Guid accountId,
        CancellationToken cancellationToken = default
    )
    {
        var actor = await GetActiveActorAsync(actorId, cancellationToken);
        EnsurePlatformAdministrator(actor);
        var account =
            await dbContext
                .Set<Account>()
                .SingleOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken)
            ?? throw new KeyNotFoundException("The account was not found.");
        account.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ActorContext> GetActiveActorAsync(
        Guid actorId,
        CancellationToken cancellationToken
    )
    {
        var actor = await dbContext
            .Set<User>()
            .SingleOrDefaultAsync(user => user.Id == actorId, cancellationToken);
        if (actor is null || actor.Status != UserStatus.Active || actor.DeactivatedAt is not null)
        {
            throw new UnauthorizedAccessException("The authenticated user is no longer active.");
        }

        var account = await dbContext
            .Set<Account>()
            .SingleOrDefaultAsync(candidate => candidate.Id == actor.AccountId, cancellationToken);
        if (account is null || account.DeactivatedAt is not null)
        {
            throw new UnauthorizedAccessException("The authenticated user is no longer active.");
        }

        return new ActorContext(actor, account);
    }

    private async Task<Account> EnsureCanManageAccountAsync(
        ActorContext actor,
        Guid accountId,
        CancellationToken cancellationToken
    )
    {
        if (
            actor.Account.Role != AccountRole.Admin
            && (actor.User.AccountId != accountId || actor.User.Role != UserRole.Admin)
        )
        {
            throw new System.Security.SecurityException("The caller cannot manage this account.");
        }

        var account = await dbContext
            .Set<Account>()
            .SingleOrDefaultAsync(candidate => candidate.Id == accountId, cancellationToken);
        if (account is null || account.DeactivatedAt is not null)
        {
            throw new KeyNotFoundException("The account was not found.");
        }

        return account;
    }

    private async Task<User> FindTargetUserAsync(
        Guid accountId,
        Guid userId,
        CancellationToken cancellationToken
    ) =>
        await dbContext
            .Set<User>()
            .SingleOrDefaultAsync(
                user => user.Id == userId && user.AccountId == accountId,
                cancellationToken
            )
        ?? throw new KeyNotFoundException("The user was not found in this account.");

    private async Task EnsureAnotherActiveAdministratorExistsAsync(
        Guid accountId,
        Guid excludedUserId,
        CancellationToken cancellationToken
    )
    {
        var count = await dbContext
            .Set<User>()
            .CountAsync(
                user =>
                    user.AccountId == accountId
                    && user.Id != excludedUserId
                    && user.Role == UserRole.Admin
                    && user.Status == UserStatus.Active
                    && user.DeactivatedAt == null,
                cancellationToken
            );
        if (count == 0)
        {
            throw LastAdministratorConflict();
        }
    }

    private static void EnsurePlatformAdministrator(ActorContext actor)
    {
        if (actor.Account.Role != AccountRole.Admin)
        {
            throw new System.Security.SecurityException("A platform administrator is required.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 128)
        {
            throw new ArgumentException("The password must contain between 12 and 128 characters.");
        }
    }

    private static ContractUserRole ToContractRole(UserRole role) =>
        role switch
        {
            UserRole.Admin => ContractUserRole.Admin,
            UserRole.User => ContractUserRole.User,
            _ => throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Unknown domain user role."
            ),
        };

    private static ContractAccountRole ToContractRole(AccountRole role) =>
        role switch
        {
            AccountRole.Admin => ContractAccountRole.Admin,
            AccountRole.User => ContractAccountRole.User,
            _ => throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Unknown domain account role."
            ),
        };

    private static ContractUserStatus ToContractStatus(UserStatus status) =>
        status switch
        {
            UserStatus.Pending => ContractUserStatus.Pending,
            UserStatus.Active => ContractUserStatus.Active,
            UserStatus.Disabled => ContractUserStatus.Disabled,
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unknown domain user status."
            ),
        };

    private static ArgumentException InvalidInvitation() =>
        new("The invitation is invalid or expired.");

    private static InvalidOperationException LastAdministratorConflict() =>
        new InvalidOperationException(
            "The change conflicts with another membership update or would leave the account without an active administrator."
        );

    private static string CreateRawToken() =>
        Convert
            .ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async Task<IDbContextTransaction?> BeginTransactionIfRelationalAsync(
        CancellationToken cancellationToken
    ) =>
        dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

    private sealed record ActorContext(User User, Account Account);
}
