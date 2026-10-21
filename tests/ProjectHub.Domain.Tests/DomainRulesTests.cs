using ProjectHub.Domain.Exceptions;
using ProjectHub.Domain.Models;

namespace ProjectHub.Domain.Tests;

public sealed class DomainRulesTests
{
    [Fact]
    public void CreateTenant_TrimsNameAndUsesConventionalRole()
    {
        var account = Account.CreateTenant("  Acme  ");

        Assert.Equal("Acme", account.Name);
        Assert.Equal(AccountRole.User, account.Role);
        Assert.NotEqual(Guid.Empty, account.Id);
    }

    [Fact]
    public void CreateOwner_NormalizesEmailAndStartsAsActiveAdministrator()
    {
        var account = Account.CreateTenant("Acme");
        var owner = User.CreateOwner(
            account.Id,
            " Ana ",
            " Silva ",
            " Ana@example.com ",
            "hashed-password"
        );

        Assert.Equal(account.Id, owner.AccountId);
        Assert.Equal(UserRole.Admin, owner.Role);
        Assert.Equal(UserStatus.Active, owner.Status);
        Assert.Equal("ANA@EXAMPLE.COM", owner.NormalizedEmail);
        Assert.Equal("Ana", owner.FirstName);
        Assert.Equal("hashed-password", owner.PasswordHash);
    }

    [Fact]
    public void CreateOwner_WithInvalidEmail_Throws()
    {
        var account = Account.CreateTenant("Acme");

        Assert.Throws<DomainValidationException>(() =>
            User.CreateOwner(account.Id, "Ana", "Silva", "not-an-email", "hashed-password")
        );
    }

    [Fact]
    public void InviteAndAccept_ConsumesInvitationExactlyOnce()
    {
        var user = User.Invite(Guid.NewGuid(), "Bia", "Costa", "bia@example.com", UserRole.User);
        var invitation = UserInvitation.Create(
            user.Id,
            "token-hash",
            DateTimeOffset.UtcNow.AddHours(1)
        );

        Assert.Equal(UserStatus.Pending, user.Status);
        Assert.True(invitation.IsUsable(DateTimeOffset.UtcNow));

        user.Activate("hashed-password");
        invitation.Accept();

        Assert.Equal(UserStatus.Active, user.Status);
        Assert.False(invitation.IsUsable(DateTimeOffset.UtcNow));
        Assert.Throws<DomainConflictException>(() => invitation.Accept());
    }

    [Fact]
    public void DisabledUser_CannotChangeRole()
    {
        var account = Account.CreateTenant("Acme");
        var owner = User.CreateOwner(
            account.Id,
            "Ana",
            "Silva",
            "ana@example.com",
            "hashed-password"
        );
        owner.Disable();

        Assert.Throws<DomainConflictException>(() => owner.ChangeRole(UserRole.User));
    }

    [Fact]
    public void DeactivatedAccount_CannotBeRenamed()
    {
        var account = Account.CreateTenant("Acme");
        account.Deactivate();

        Assert.Throws<DomainConflictException>(() => account.Rename("Renamed"));
    }
}
