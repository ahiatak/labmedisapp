using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LABMEDIS.Api.Controllers;

/// <summary>Authentication (US2 — contracts/auth.md). FR-012 à FR-019.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IUserService userService, ILoggerManager logger) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        logger.LogInfo(HttpContext.BuildStartLog(null, nameof(Login)));
        try
        {
            var response = await userService.LoginAsync(request, HttpContext.GetIp(), HttpContext.GetUserAgentName(), cancellationToken);
            return Ok(response);
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(null, nameof(Login), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(null, nameof(Login), ex.Message));
            return BadRequest(new { message = "La connexion a échoué." });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        logger.LogInfo(HttpContext.BuildStartLog(null, nameof(Refresh)));
        try
        {
            var response = await userService.RefreshAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(null, nameof(Refresh), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(null, nameof(Refresh), ex.Message));
            return BadRequest(new { message = "Le renouvellement du jeton a échoué." });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Logout)));
        try
        {
            await userService.LogoutAsync(request, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Logout), ex.Message));
            return BadRequest(new { message = "La déconnexion a échoué." });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        logger.LogInfo(HttpContext.BuildStartLog(null, nameof(ForgotPassword)));
        try
        {
            await userService.ForgotPasswordAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(null, nameof(ForgotPassword), ex.Message));
        }

        // Generic response regardless of outcome — never reveal whether the email exists.
        return Ok(new { message = "Si cet email existe, un lien de réinitialisation a été envoyé." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        logger.LogInfo(HttpContext.BuildStartLog(null, nameof(ResetPassword)));
        try
        {
            await userService.ResetPasswordAsync(request, cancellationToken);
            return Ok(new { message = "Mot de passe réinitialisé." });
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(null, nameof(ResetPassword), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(null, nameof(ResetPassword), ex.Message));
            return BadRequest(new { message = "La réinitialisation du mot de passe a échoué." });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Me)));
        if (currentUser is null)
        {
            return Unauthorized();
        }

        return Ok(currentUser);
    }
}
