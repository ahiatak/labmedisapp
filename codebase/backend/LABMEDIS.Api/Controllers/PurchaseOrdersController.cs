using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Commandes d'achat (US3 — contracts/purchase-orders.md, FR-020 à FR-024).</summary>
[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrdersController(IPurchaseOrderService purchaseOrderService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "PurchaseOrders.Read")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] Guid? supplierId, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAll)));
        try
        {
            return Ok(await purchaseOrderService.ListAsync(status, supplierId, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAll), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les commandes d'achat." });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "PurchaseOrders.Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetById)));
        try
        {
            var order = await purchaseOrderService.GetAsync(id, cancellationToken);
            return order is null ? NotFound(new { message = "Commande d'achat introuvable." }) : Ok(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetById), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer cette commande d'achat." });
        }
    }

    [HttpGet("{id:guid}/status-history")]
    [Authorize(Policy = "PurchaseOrders.Read")]
    public async Task<IActionResult> GetStatusHistory(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetStatusHistory)));
        try
        {
            return Ok(await purchaseOrderService.GetStatusHistoryAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetStatusHistory), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer l'historique de cette commande." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "PurchaseOrders.Create")]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Create)));
        try
        {
            var created = await purchaseOrderService.CreateAsync(request, cancellationToken);
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
            return BadRequest(new { message = "Impossible de créer cette commande d'achat." });
        }
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = "PurchaseOrders.Create")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Submit)));
        try
        {
            return Ok(await purchaseOrderService.SubmitAsync(id, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Submit), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Submit), ex.Message));
            return BadRequest(new { message = "Impossible de soumettre cette commande d'achat." });
        }
    }

    [HttpPost("{id:guid}/validate")]
    [Authorize(Policy = "PurchaseOrders.Validate")]
    public async Task<IActionResult> Validate(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Validate)));
        try
        {
            var callerIsDirection = User.IsInRole("Direction") || User.IsInRole("Admin");
            return Ok(await purchaseOrderService.ValidateAsync(id, currentUser!.Id, callerIsDirection, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Validate), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Validate), ex.Message));
            return BadRequest(new { message = "Impossible de valider cette commande d'achat." });
        }
    }

    [HttpPost("{id:guid}/receive")]
    [Authorize(Policy = "Stock.Receive")]
    public async Task<IActionResult> Receive(Guid id, [FromBody] List<ReceiveLotLineRequest> lines, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Receive)));
        try
        {
            return Ok(await purchaseOrderService.ReceiveAsync(id, lines, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Receive), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Receive), ex.Message));
            return BadRequest(new { message = "La réception de cette commande a échoué." });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "PurchaseOrders.Create")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Cancel)));
        try
        {
            return Ok(await purchaseOrderService.CancelAsync(id, currentUser!.Id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Cancel), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Cancel), ex.Message));
            return BadRequest(new { message = "Impossible d'annuler cette commande d'achat." });
        }
    }
}
