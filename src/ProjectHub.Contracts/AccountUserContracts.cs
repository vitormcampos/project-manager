using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Contracts;

/// <summary>Information returned for an account member.</summary>
public sealed record UserSummary(
    Guid Id,
    Guid AccountId,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    UserStatus Status
);

/// <summary>Information returned for an account.</summary>
public sealed record AccountSummary(
    Guid Id,
    string Name,
    AccountRole Role,
    DateTimeOffset CreatedAt
);

/// <summary>Information returned after creating a one-time invitation.</summary>
public sealed record InvitationCreated(Guid UserId, DateTimeOffset ExpiresAt);

/// <summary>Request body for accepting an account invitation.</summary>
public sealed record AcceptInvitationRequest(
    [property: Required] string Token,
    [property: Required, MinLength(12), MaxLength(128)] string Password
);

/// <summary>Request body for inviting a member to an account.</summary>
public sealed record InviteUserRequest(
    [property: Required, StringLength(100)] string FirstName,
    [property: Required, StringLength(100)] string LastName,
    [property: Required, EmailAddress, StringLength(320)] string Email,
    [property: EnumDataType(typeof(UserRole))] UserRole Role = UserRole.User
);

/// <summary>Request body for changing a member's account-scoped role.</summary>
public sealed record ChangeUserRoleRequest(
    [property: EnumDataType(typeof(UserRole))] UserRole Role
);
