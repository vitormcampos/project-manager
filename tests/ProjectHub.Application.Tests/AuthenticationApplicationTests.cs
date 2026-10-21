using Microsoft.EntityFrameworkCore;
using ProjectHub.Application.Contracts;
using ProjectHub.Application.Services;
using ProjectHub.Data;
using ProjectHub.Domain.Models;
using LoginRequest = ProjectHub.Contracts.LoginRequest;
using RegisterAccountRequest = ProjectHub.Contracts.RegisterAccountRequest;

namespace ProjectHub.Application.Tests;

public sealed class AuthenticationApplicationTests
    : IClassFixture<PostgresTestDatabaseFixture>,
        IAsyncLifetime
{
    private readonly PostgresTestDatabaseFixture _database;

    public AuthenticationApplicationTests(PostgresTestDatabaseFixture database)
    {
        _database = database;
    }

    public Task InitializeAsync() => _database.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RegisterAccount_CreatesTenantAndOwnerAndReturnsToken()
    {
        await using var dbContext = CreateDbContext();
        var service = new AuthenticationService(
            dbContext,
            new TestPasswordHasher(),
            new TestTokenIssuer()
        );

        var result = await service.RegisterAccountAsync(
            new RegisterAccountRequest(
                "Acme",
                "Ana",
                "Silva",
                "ana@example.com",
                "long-password-123"
            )
        );

        var account = await dbContext.Set<Account>().SingleAsync();
        var owner = await dbContext.Set<User>().SingleAsync();
        Assert.Equal(account.Id, owner.AccountId);
        Assert.Equal(AccountRole.User, account.Role);
        Assert.Equal(UserRole.Admin, owner.Role);
        Assert.Equal(UserStatus.Active, owner.Status);
        Assert.Equal(owner.Id, result.UserId);
        Assert.Equal("access-token", result.AccessToken.Value);
        Assert.NotEqual("long-password-123", owner.PasswordHash);
    }

    [Fact]
    public async Task RegisterAccount_WithDuplicateNormalizedEmail_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();
        var service = new AuthenticationService(
            dbContext,
            new TestPasswordHasher(),
            new TestTokenIssuer()
        );
        await service.RegisterAccountAsync(
            new RegisterAccountRequest(
                "Acme",
                "Ana",
                "Silva",
                "ana@example.com",
                "long-password-123"
            )
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAccountAsync(
                new RegisterAccountRequest(
                    "Other",
                    "Another",
                    "Owner",
                    " ANA@EXAMPLE.COM ",
                    "long-password-123"
                )
            )
        );

        Assert.Equal("The email is already registered.", exception.Message);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new TestPasswordHasher();
        var account = Account.CreateTenant("Acme");
        var owner = User.CreateOwner(
            account.Id,
            "Ana",
            "Silva",
            "ana@example.com",
            hasher.Hash("long-password-123")
        );
        dbContext.Set<Account>().Add(account);
        dbContext.Set<User>().Add(owner);
        await dbContext.SaveChangesAsync();
        var service = new AuthenticationService(dbContext, hasher, new TestTokenIssuer());

        var result = await service.LoginAsync(
            new LoginRequest("ANA@example.com", "long-password-123")
        );

        Assert.Equal(owner.Id, result.UserId);
        Assert.Equal(account.Id, result.AccountId);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsGenericUnauthorizedError()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new TestPasswordHasher();
        var account = Account.CreateTenant("Acme");
        var owner = User.CreateOwner(
            account.Id,
            "Ana",
            "Silva",
            "ana@example.com",
            hasher.Hash("long-password-123")
        );
        dbContext.Set<Account>().Add(account);
        dbContext.Set<User>().Add(owner);
        await dbContext.SaveChangesAsync();
        var service = new AuthenticationService(dbContext, hasher, new TestTokenIssuer());

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new LoginRequest("ana@example.com", "wrong-password"))
        );

        Assert.Equal("The email or password is invalid.", exception.Message);
    }

    [Fact]
    public async Task TenantAdministrator_CannotReadUsersFromAnotherAccount()
    {
        await using var dbContext = CreateDbContext();
        var firstAccount = Account.CreateTenant("First");
        var firstOwner = User.CreateOwner(
            firstAccount.Id,
            "First",
            "Owner",
            "first@example.com",
            "hash"
        );
        var secondAccount = Account.CreateTenant("Second");
        var secondOwner = User.CreateOwner(
            secondAccount.Id,
            "Second",
            "Owner",
            "second@example.com",
            "hash"
        );
        dbContext.Set<Account>().AddRange(firstAccount, secondAccount);
        dbContext.Set<User>().AddRange(firstOwner, secondOwner);
        await dbContext.SaveChangesAsync();
        var service = new AccountUserService(
            dbContext,
            new TestPasswordHasher(),
            new TestTokenIssuer(),
            new TestInvitationDelivery()
        );

        var exception = await Assert.ThrowsAsync<System.Security.SecurityException>(() =>
            service.GetUsersAsync(firstOwner.Id, secondAccount.Id)
        );

        Assert.Equal("The caller cannot manage this account.", exception.Message);
    }

    [Fact]
    public async Task TenantAdministrator_CannotDisableTheLastActiveAdministrator()
    {
        await using var dbContext = CreateDbContext();
        var account = Account.CreateTenant("Acme");
        var owner = User.CreateOwner(account.Id, "Ana", "Silva", "ana@example.com", "hash");
        dbContext.Set<Account>().Add(account);
        dbContext.Set<User>().Add(owner);
        await dbContext.SaveChangesAsync();
        var service = new AccountUserService(
            dbContext,
            new TestPasswordHasher(),
            new TestTokenIssuer(),
            new TestInvitationDelivery()
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DisableUserAsync(owner.Id, account.Id, owner.Id)
        );

        Assert.Contains("without an active administrator", exception.Message);
        Assert.Equal(UserStatus.Active, owner.Status);
    }

    [Fact]
    public async Task InviteAndAccept_ActivatesUserAndConsumesToken()
    {
        await using var dbContext = CreateDbContext();
        var account = Account.CreateTenant("Acme");
        var owner = User.CreateOwner(account.Id, "Ana", "Silva", "ana@example.com", "hash");
        dbContext.Set<Account>().Add(account);
        dbContext.Set<User>().Add(owner);
        await dbContext.SaveChangesAsync();
        var delivery = new TestInvitationDelivery();
        var hasher = new TestPasswordHasher();
        var service = new AccountUserService(dbContext, hasher, new TestTokenIssuer(), delivery);

        var invitation = await service.InviteUserAsync(
            owner.Id,
            account.Id,
            "Bia",
            "Costa",
            "bia@example.com",
            UserRole.User
        );
        Assert.NotNull(delivery.Token);
        var rawToken = delivery.Token!;
        var result = await service.AcceptInvitationAsync(rawToken, "another-long-password");
        var invitedUser = await dbContext
            .Set<User>()
            .SingleAsync(user => user.Id == invitation.UserId);

        Assert.Equal(UserStatus.Active, invitedUser.Status);
        Assert.Equal("another-long-password", hasher.LastPassword);
        Assert.Equal(invitedUser.Id, result.UserId);
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AcceptInvitationAsync(rawToken, "another-long-password")
        );
        Assert.Equal("The invitation is invalid or expired.", exception.Message);
    }

    [Fact]
    public async Task RevokeInvitation_PreventsTheInviteeFromActivating()
    {
        await using var dbContext = CreateDbContext();
        var account = Account.CreateTenant("Acme");
        var owner = User.CreateOwner(account.Id, "Ana", "Silva", "ana@example.com", "hash");
        dbContext.Set<Account>().Add(account);
        dbContext.Set<User>().Add(owner);
        await dbContext.SaveChangesAsync();
        var delivery = new TestInvitationDelivery();
        var service = new AccountUserService(
            dbContext,
            new TestPasswordHasher(),
            new TestTokenIssuer(),
            delivery
        );

        var invitation = await service.InviteUserAsync(
            owner.Id,
            account.Id,
            "Bia",
            "Costa",
            "bia@example.com",
            UserRole.User
        );
        await service.RevokeInvitationAsync(owner.Id, account.Id, invitation.UserId);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AcceptInvitationAsync(delivery.Token!, "another-long-password")
        );
        Assert.Equal("The invitation is invalid or expired.", exception.Message);
        var persistedInvitation = await dbContext
            .Set<UserInvitation>()
            .SingleAsync(item => item.UserId == invitation.UserId);
        Assert.NotNull(persistedInvitation.RevokedAt);
    }

    [Fact]
    public async Task UserInPlatformAccount_CanManageConventionalTenantRegardlessOfUserRole()
    {
        await using var dbContext = CreateDbContext();
        var platform = Account.CreatePlatformAccount("Platform");
        var platformUser = User.Invite(
            platform.Id,
            "Support",
            "Member",
            "support@example.com",
            UserRole.User
        );
        platformUser.Activate("hash");
        var tenant = Account.CreateTenant("Tenant");
        var tenantOwner = User.CreateOwner(
            tenant.Id,
            "Tenant",
            "Owner",
            "owner@example.com",
            "hash"
        );
        dbContext.Set<Account>().AddRange(platform, tenant);
        dbContext.Set<User>().AddRange(platformUser, tenantOwner);
        await dbContext.SaveChangesAsync();
        var service = new AccountUserService(
            dbContext,
            new TestPasswordHasher(),
            new TestTokenIssuer(),
            new TestInvitationDelivery()
        );

        var users = await service.GetUsersAsync(platformUser.Id, tenant.Id);

        Assert.Single(users);
        Assert.Equal(tenantOwner.Id, users[0].Id);
    }

    private ProjectHubDbContext CreateDbContext() => _database.CreateDbContext();

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string? LastPassword { get; private set; }

        public string Hash(string password)
        {
            LastPassword = password;
            return $"test-hash:{password}";
        }

        public bool Verify(string passwordHash, string password) =>
            passwordHash == HashValue(password);

        private static string HashValue(string password) => $"test-hash:{password}";
    }

    private sealed class TestTokenIssuer : IAccessTokenIssuer
    {
        public AccessToken Issue(Guid userId, Guid accountId) =>
            new("access-token", DateTimeOffset.UtcNow.AddMinutes(15));
    }

    private sealed class TestInvitationDelivery : IInvitationDelivery
    {
        public string? Token { get; private set; }

        public Task SendAsync(
            string email,
            string firstName,
            string rawToken,
            CancellationToken cancellationToken
        )
        {
            Token = rawToken;
            return Task.CompletedTask;
        }
    }
}
