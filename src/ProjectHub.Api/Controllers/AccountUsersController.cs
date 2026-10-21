using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectHub.Api.Infrastructure;
using ProjectHub.Application.Services;
using ProjectHub.Contracts;
using DomainUserRole = ProjectHub.Domain.Models.UserRole;

namespace ProjectHub.Api.Controllers;

/// <summary>Provides tenant-scoped member management endpoints.</summary>
[ApiController]
[Authorize]
[Route("api/accounts/{accountId:guid}/users")]
public sealed class AccountUsersController(AccountUserService accountUserService) : ControllerBase
{
    /// <summary>Lists members in an account for an authorized administrator.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSummary>>> GetUsers(
        Guid accountId,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await accountUserService.GetUsersAsync(
                CurrentIdentity.GetUserId(User),
                accountId,
                cancellationToken
            )
        );

    /// <summary>Invites a user to the specified account.</summary>
    [HttpPost("invitations")]
    [ProducesResponseType<InvitationCreated>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InvitationCreated>> InviteUser(
        Guid accountId,
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await accountUserService.InviteUserAsync(
                CurrentIdentity.GetUserId(User),
                accountId,
                request.FirstName,
                request.LastName,
                request.Email,
                ToDomainRole(request.Role),
                cancellationToken
            )
        );

    /// <summary>Revokes an unused invitation for a pending member.</summary>
    [HttpDelete("{userId:guid}/invitation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeInvitation(
        Guid accountId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        await accountUserService.RevokeInvitationAsync(
            CurrentIdentity.GetUserId(User),
            accountId,
            userId,
            cancellationToken
        );
        return NoContent();
    }

    /// <summary>Changes a member's account-scoped role.</summary>
    [HttpPut("{userId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeRole(
        Guid accountId,
        Guid userId,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken
    )
    {
        await accountUserService.ChangeUserRoleAsync(
            CurrentIdentity.GetUserId(User),
            accountId,
            userId,
            ToDomainRole(request.Role),
            cancellationToken
        );
        return NoContent();
    }

    /// <summary>Disables a member in the account.</summary>
    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DisableUser(
        Guid accountId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        await accountUserService.DisableUserAsync(
            CurrentIdentity.GetUserId(User),
            accountId,
            userId,
            cancellationToken
        );
        return NoContent();
    }

    private static DomainUserRole ToDomainRole(UserRole role) =>
        role switch
        {
            UserRole.Admin => DomainUserRole.Admin,
            UserRole.User => DomainUserRole.User,
            _ => throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Unknown contract user role."
            ),
        };
}
