using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectHub.Api.Infrastructure;
using ProjectHub.Application.Services;
using ProjectHub.Contracts;

namespace ProjectHub.Api.Controllers;

/// <summary>Provides cross-tenant account administration for platform administrators.</summary>
[ApiController]
[Authorize]
[Route("api/admin/accounts")]
public sealed class SystemAccountsController(AccountUserService accountUserService) : ControllerBase
{
    /// <summary>Lists all accounts for a platform administrator.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccountSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountSummary>>> GetAccounts(
        CancellationToken cancellationToken
    ) =>
        Ok(
            await accountUserService.GetAccountsAsync(
                CurrentIdentity.GetUserId(User),
                cancellationToken
            )
        );

    /// <summary>Deactivates an account and prevents its members from authenticating.</summary>
    [HttpDelete("{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateAccount(
        Guid accountId,
        CancellationToken cancellationToken
    )
    {
        await accountUserService.DeactivateAccountAsync(
            CurrentIdentity.GetUserId(User),
            accountId,
            cancellationToken
        );
        return NoContent();
    }
}
