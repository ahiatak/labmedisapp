using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Notifications persistées (US12 — contracts/notifications.md, FR-078/FR-094).</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool unreadOnly, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAll)));
        try
        {
            return Ok(await notificationService.GetForUserAsync(currentUser!.Id, currentUser.Roles, currentUser.Permissions, unreadOnly, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAll), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les notifications." });
        }
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(MarkRead)));
        try
        {
            await notificationService.MarkReadAsync(id, currentUser!.Id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(MarkRead), ex.Message));
            return BadRequest(new { message = "Impossible de marquer cette notification comme lue." });
        }
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(MarkAllRead)));
        try
        {
            await notificationService.MarkAllReadAsync(currentUser!.Id, currentUser.Roles, currentUser.Permissions, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(MarkAllRead), ex.Message));
            return BadRequest(new { message = "Impossible de marquer toutes les notifications comme lues." });
        }
    }
}
