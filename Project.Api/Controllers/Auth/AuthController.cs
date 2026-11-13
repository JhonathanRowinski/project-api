using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Api.DTOs.Auth;
using Project.Application.Auth.Models;
using Project.Application.Auth.Interfaces;

namespace Project.Api.Controllers.Auth;

/// <summary>Exposes authentication endpoints to HTTP clients.</summary>
/// <param name="loginService">Application service that performs authentication operations.</param>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ILoginService loginService) : ControllerBase
{
    /// <summary>Authenticates the submitted account credentials and returns tokens.</summary>
    /// <param name="request">Email, password, platform, and device identifier.</param>
    /// <returns>An HTTP response containing the issued tokens.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        var tokens = await loginService.LoginAsync(request.Email, request.Password);

        return Ok(CreateResponse(tokens));
    }

    /// <summary>Rotates a refresh token and returns a new token pair.</summary>
    /// <param name="request">The refresh token submitted by the client.</param>
    /// <returns>An HTTP response containing the rotated tokens.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDTO request)
    {
        var tokens = await loginService.RefreshAsync(request.RefreshToken);

        return Ok(CreateResponse(tokens));
    }

    /// <summary>Revokes the authenticated session identified by the access token.</summary>
    /// <returns>An HTTP 204 response after the session is revoked.</returns>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Logout()
    {
        await loginService.RevokeSessionAsync(User.FindFirst("sid")?.Value);

        return NoContent();
    }

    /// <summary>Maps application token data to the HTTP response DTO.</summary>
    /// <param name="tokens">Tokens returned by the application service.</param>
    /// <returns>The login response contract returned to the client.</returns>
    private static LoginResponseDTO CreateResponse(AuthTokens tokens) => new(
        tokens.AccessToken.Value,
        tokens.RefreshToken,
        "Bearer",
        tokens.AccessToken.ExpiresAtUtc,
        tokens.RefreshTokenExpiresAtUtc);
}