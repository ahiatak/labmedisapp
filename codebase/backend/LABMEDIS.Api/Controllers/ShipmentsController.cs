using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Expéditions / Logistique (US3 — contracts/shipments.md, FR-025 à FR-028).</summary>
[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController(IShipmentService shipmentService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Shipments.Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetById)));
        try
        {
            var shipment = await shipmentService.GetAsync(id, cancellationToken);
            return shipment is null ? NotFound(new { message = "Expédition introuvable." }) : Ok(shipment);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetById), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer cette expédition." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "Shipments.Create")]
    public async Task<IActionResult> Create([FromBody] CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Create)));
        try
        {
            var created = await shipmentService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Create), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Create), ex.Message));
            return BadRequest(new { message = "Impossible de créer cette expédition." });
        }
    }

    [HttpPost("{id:guid}/costs")]
    [Authorize(Policy = "Shipments.Update")]
    public async Task<IActionResult> AddCost(Guid id, [FromBody] AddImportCostRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(AddCost)));
        try
        {
            return Ok(await shipmentService.AddCostAsync(id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddCost), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddCost), ex.Message));
            return BadRequest(new { message = "Impossible d'ajouter ce frais logistique." });
        }
    }

    [HttpPost("{id:guid}/events")]
    [Authorize(Policy = "Shipments.Update")]
    public async Task<IActionResult> AddEvent(Guid id, [FromBody] AddShipmentEventRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(AddEvent)));
        try
        {
            return Ok(await shipmentService.AddEventAsync(id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddEvent), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddEvent), ex.Message));
            return BadRequest(new { message = "Impossible d'enregistrer cet événement de suivi." });
        }
    }

    [HttpGet("{id:guid}/timeline")]
    [Authorize(Policy = "Shipments.Read")]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetTimeline)));
        try
        {
            return Ok(await shipmentService.GetTimelineAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetTimeline), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer le suivi de cette expédition." });
        }
    }
}
