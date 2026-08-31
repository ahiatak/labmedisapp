using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Conformité documentaire et traçabilité de rappel (US13 — FR-080 à FR-083).</summary>
[ApiController]
[Route("api/compliance")]
[Authorize]
public class ComplianceController(IComplianceService complianceService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpPost("attachments")]
    [Authorize(Policy = "Compliance.Manage")]
    public async Task<IActionResult> AddAttachment([FromBody] CreateAttachmentRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(AddAttachment)));
        try
        {
            return Ok(await complianceService.AddAttachmentAsync(request, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddAttachment), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddAttachment), ex.Message));
            return BadRequest(new { message = "Impossible de rattacher cette pièce justificative." });
        }
    }

    [HttpGet("attachments")]
    [Authorize(Policy = "Compliance.Read")]
    public async Task<IActionResult> GetAttachments([FromQuery] string attachableType, [FromQuery] Guid attachableId, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAttachments)));
        try
        {
            return Ok(await complianceService.GetAttachmentsAsync(attachableType, attachableId, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAttachments), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les pièces justificatives." });
        }
    }

    [HttpGet("lots/{id:guid}/traceability")]
    [Authorize(Policy = "Compliance.Read")]
    public async Task<IActionResult> GetLotTraceability(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetLotTraceability)));
        try
        {
            return Ok(await complianceService.GetLotTraceabilityAsync(id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetLotTraceability), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetLotTraceability), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer la traçabilité de ce lot." });
        }
    }
}
