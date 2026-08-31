using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>User account administration (US2 — FR-012 à FR-019).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService, ILoggerManager logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Users.Read")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAll)));
        try
        {
            return Ok(await userService.GetAllAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAll), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer la liste des utilisateurs." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "Users.Create")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Create)));
        try
        {
            var created = await userService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAll), created);
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Create), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Create), ex.Message));
            return BadRequest(new { message = "Impossible de créer cet utilisateur." });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Users.Delete")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Deactivate)));
        try
        {
            await userService.DeactivateAsync(id, cancellationToken);
            return NoContent();
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Deactivate), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Deactivate), ex.Message));
            return BadRequest(new { message = "Impossible de désactiver cet utilisateur." });
        }
    }

    [HttpPut("{id:guid}/permission-exceptions")]
    [Authorize(Policy = "Users.Update")]
    public async Task<IActionResult> SetPermissionExceptions(Guid id, [FromBody] UpdateUserPermissionExceptionsRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(SetPermissionExceptions)));
        try
        {
            await userService.SetPermissionExceptionsAsync(id, request, cancellationToken);
            return NoContent();
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(SetPermissionExceptions), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(SetPermissionExceptions), ex.Message));
            return BadRequest(new { message = "Impossible de mettre à jour les dérogations de permission." });
        }
    }
}
