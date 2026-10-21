using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProjectHub.Application.Services;
using ProjectHub.Contracts;

namespace ProjectHub.Api.Controllers;

/// <summary>Provides account registration and authentication endpoints.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController(
    AuthenticationService authenticationService,
    AccountUserService accountUserService
) : ControllerBase
{
    /// <summary>Registers a conventional account and its initial administrator.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<AuthenticationResult>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AuthenticationResult>> Register(
        [FromBody] RegisterAccountRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await authenticationService.RegisterAccountAsync(request, cancellationToken)
        );

    /// <summary>Authenticates an active account user.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType<AuthenticationResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResult>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken
    ) => Ok(await authenticationService.LoginAsync(request, cancellationToken));

    /// <summary>Accepts a valid one-time invitation and activates the user.</summary>
    [AllowAnonymous]
    [HttpPost("invitations/accept")]
    [ProducesResponseType<AuthenticationResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResult>> AcceptInvitation(
        [FromBody] AcceptInvitationRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await accountUserService.AcceptInvitationAsync(
                request.Token,
                request.Password,
                cancellationToken
            )
        );
}
