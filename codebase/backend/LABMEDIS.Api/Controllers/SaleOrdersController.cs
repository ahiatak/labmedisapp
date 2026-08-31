using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Commandes de vente et facturation (US7 — contracts/sales.md, FR-054 à FR-059).</summary>
[ApiController]
[Route("api/sale-orders")]
[Authorize]
public class SaleOrdersController(
    ISaleOrderService saleOrderService, IStockLotService stockLotService, IInvoicePdfService invoicePdfService,
    ICustomerReturnService customerReturnService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Sales.Read")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAll)));
        try
        {
            return Ok(await saleOrderService.ListAsync(status, customerId, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAll), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les commandes de vente." });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Sales.Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetById)));
        try
        {
            var order = await saleOrderService.GetAsync(id, cancellationToken);
            return order is null ? NotFound(new { message = "Commande de vente introuvable." }) : Ok(order);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetById), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer cette commande de vente." });
        }
    }

    [HttpGet("{id:guid}/fefo-suggestion")]
    [Authorize(Policy = "Sales.Read")]
    public async Task<IActionResult> GetFefoSuggestion(Guid id, [FromQuery] Guid productId, [FromQuery] int quantity, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetFefoSuggestion)));
        try
        {
            return Ok(await stockLotService.GetFefoSuggestionAsync(productId, quantity, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetFefoSuggestion), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetFefoSuggestion), ex.Message));
            return BadRequest(new { message = "Impossible de calculer la suggestion FEFO." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "Sales.Create")]
    public async Task<IActionResult> Create([FromBody] CreateSaleOrderRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Create)));
        try
        {
            var created = await saleOrderService.CreateAsync(request, currentUser!.Id, cancellationToken);
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
            return BadRequest(new { message = "Impossible de créer cette commande de vente." });
        }
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = "Sales.Create")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Confirm)));
        try
        {
            return Ok(await saleOrderService.ConfirmAsync(id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Confirm), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Confirm), ex.Message));
            return BadRequest(new { message = "Impossible de confirmer cette commande de vente." });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Sales.Create")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Cancel)));
        try
        {
            return Ok(await saleOrderService.CancelAsync(id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Cancel), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Cancel), ex.Message));
            return BadRequest(new { message = "Impossible d'annuler cette commande de vente." });
        }
    }

    [HttpPost("{id:guid}/deliver")]
    [Authorize(Policy = "Sales.Deliver")]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Deliver)));
        try
        {
            return Ok(await saleOrderService.DeliverAsync(id, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Deliver), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Deliver), ex.Message));
            return BadRequest(new { message = "Impossible de livrer cette commande de vente." });
        }
    }

    [HttpPost("{id:guid}/invoice")]
    [Authorize(Policy = "Sales.Invoice")]
    public async Task<IActionResult> Invoice(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Invoice)));
        try
        {
            return Ok(await saleOrderService.InvoiceAsync(id, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Invoice), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Invoice), ex.Message));
            return BadRequest(new { message = "Impossible de facturer cette commande de vente." });
        }
    }

    [HttpPost("{id:guid}/returns")]
    [Authorize(Policy = "Returns.Create")]
    public async Task<IActionResult> CreateReturn(Guid id, [FromBody] CreateReturnRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(CreateReturn)));
        try
        {
            var created = await customerReturnService.CreateAsync(id, request, currentUser!.Id, cancellationToken);
            return StatusCode(201, created);
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateReturn), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateReturn), ex.Message));
            return BadRequest(new { message = "Impossible de créer ce retour." });
        }
    }

    [HttpGet("{id:guid}/returns")]
    [Authorize(Policy = "Returns.Read")]
    public async Task<IActionResult> GetReturns(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetReturns)));
        try
        {
            return Ok(await customerReturnService.ListBySaleOrderAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetReturns), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les retours de cette commande." });
        }
    }

    [HttpGet("{id:guid}/invoice/pdf")]
    [Authorize(Policy = "Sales.Read")]
    public async Task<IActionResult> GetInvoicePdf(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetInvoicePdf)));
        try
        {
            var pdfBytes = await invoicePdfService.GenerateInvoicePdfAsync(id, cancellationToken);
            return File(pdfBytes, "application/pdf", $"facture-{id}.pdf");
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetInvoicePdf), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetInvoicePdf), ex.Message));
            return BadRequest(new { message = "Impossible de générer le PDF de la facture." });
        }
    }
}
