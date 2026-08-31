using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Stock, traçabilité FEFO et qualité (US4/US5 — contracts/stock.md, FR-029 à FR-044).</summary>
[ApiController]
[Route("api/stock")]
[Authorize]
public class StockController(
    IStockLotService stockLotService, IStockMovementService stockMovementService, IInventorySessionService inventorySessionService,
    ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet("available")]
    [Authorize(Policy = "Stock.Read")]
    public async Task<IActionResult> GetAvailable([FromQuery] Guid productId, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAvailable)));
        try
        {
            return Ok(await stockLotService.GetAvailableAsync(productId, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAvailable), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer le stock disponible." });
        }
    }

    [HttpGet("lots")]
    [Authorize(Policy = "Stock.Read")]
    public async Task<IActionResult> GetLots([FromQuery] Guid? productId, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetLots)));
        try
        {
            var lots = await stockLotService.ListAsync(productId, cancellationToken);
            if (!CanViewCost())
            {
                foreach (var lot in lots)
                {
                    lot.UnitCostCfa = null;
                }
            }

            return Ok(lots);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetLots), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les lots." });
        }
    }

    [HttpGet("lots/{id:guid}")]
    [Authorize(Policy = "Stock.Read")]
    public async Task<IActionResult> GetLot(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetLot)));
        try
        {
            var lot = await stockLotService.GetAsync(id, cancellationToken);
            if (lot is null)
            {
                return NotFound(new { message = "Lot introuvable." });
            }

            if (!CanViewCost())
            {
                lot.UnitCostCfa = null;
            }

            return Ok(lot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetLot), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer ce lot." });
        }
    }

    [HttpGet("fefo-suggestion")]
    [Authorize(Policy = "Stock.Read")]
    public async Task<IActionResult> GetFefoSuggestion([FromQuery] Guid productId, [FromQuery] int quantity, CancellationToken cancellationToken)
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

    [HttpPost("lots/allocate")]
    [Authorize(Policy = "Stock.Move")]
    public async Task<IActionResult> Allocate([FromBody] AllocateLotRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Allocate)));
        try
        {
            return Ok(await stockLotService.AllocateAsync(request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Allocate), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Allocate), ex.Message));
            return BadRequest(new { message = "Impossible d'allouer ce lot." });
        }
    }

    [HttpPost("movements")]
    [Authorize(Policy = "Stock.Move")]
    public async Task<IActionResult> RecordMovement([FromBody] RecordMovementRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(RecordMovement)));
        try
        {
            return Ok(await stockMovementService.RecordAsync(request, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(RecordMovement), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(RecordMovement), ex.Message));
            return BadRequest(new { message = "Impossible d'enregistrer ce mouvement." });
        }
    }

    [HttpPost("lots/{id:guid}/quarantine")]
    [Authorize(Policy = "Stock.Move")]
    public async Task<IActionResult> Quarantine(Guid id, [FromBody] QuarantineLotRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Quarantine)));
        try
        {
            return Ok(await stockLotService.QuarantineAsync(id, currentUser!.Id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Quarantine), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Quarantine), ex.Message));
            return BadRequest(new { message = "Impossible de mettre ce lot en quarantaine." });
        }
    }

    [HttpPost("lots/{id:guid}/release")]
    [Authorize(Policy = "Quality.Release")]
    public async Task<IActionResult> Release(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Release)));
        try
        {
            return Ok(await stockLotService.ReleaseAsync(id, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Release), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Release), ex.Message));
            return BadRequest(new { message = "Impossible de libérer ce lot." });
        }
    }

    [HttpPost("lots/{id:guid}/non-conforme")]
    [Authorize(Policy = "Quality.Release")]
    public async Task<IActionResult> MarkNonConforme(Guid id, [FromBody] RejectLotRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(MarkNonConforme)));
        try
        {
            return Ok(await stockLotService.MarkNonConformeAsync(id, currentUser!.Id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(MarkNonConforme), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(MarkNonConforme), ex.Message));
            return BadRequest(new { message = "Impossible de rejeter ce lot." });
        }
    }

    [HttpPost("lots/{id:guid}/destroy")]
    [Authorize(Policy = "Quality.Release")]
    public async Task<IActionResult> Destroy(Guid id, [FromBody] DestroyLotRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Destroy)));

        if (!User.IsInRole("Admin"))
        {
            logger.LogError(new AppException(403, "ADMIN_REQUIRED", "réservé"), HttpContext.BuildErrorLog(currentUser, nameof(Destroy), "Admin requis"));
            return StatusCode(403, new { message = "La destruction d'un lot requiert également le rôle Admin.", code = "ADMIN_REQUIRED" });
        }

        try
        {
            return Ok(await stockLotService.DestroyAsync(id, currentUser!.Id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Destroy), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Destroy), ex.Message));
            return BadRequest(new { message = "Impossible de détruire ce lot." });
        }
    }

    [HttpPost("lots/{id:guid}/suspected-falsified")]
    [Authorize(Roles = "Admin,Direction")]
    public async Task<IActionResult> SuspectFalsified(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(SuspectFalsified)));
        try
        {
            return Ok(await stockLotService.SuspectFalsifiedAsync(id, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(SuspectFalsified), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(SuspectFalsified), ex.Message));
            return BadRequest(new { message = "Impossible de signaler ce lot." });
        }
    }

    [HttpPost("inventory-sessions")]
    [Authorize(Policy = "Inventory.Manage")]
    public async Task<IActionResult> CreateInventorySession([FromBody] CreateInventorySessionRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(CreateInventorySession)));
        try
        {
            var created = await inventorySessionService.CreateAsync(request, currentUser!.Id, cancellationToken);
            return CreatedAtAction(nameof(GetInventorySession), new { id = created.Id }, created);
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateInventorySession), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateInventorySession), ex.Message));
            return BadRequest(new { message = "Impossible de créer cette session d'inventaire." });
        }
    }

    [HttpGet("inventory-sessions/{id:guid}")]
    [Authorize(Policy = "Inventory.Manage")]
    public async Task<IActionResult> GetInventorySession(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetInventorySession)));
        try
        {
            var session = await inventorySessionService.GetAsync(id, cancellationToken);
            return session is null ? NotFound(new { message = "Session d'inventaire introuvable." }) : Ok(session);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetInventorySession), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer cette session d'inventaire." });
        }
    }

    [HttpPost("inventory-sessions/{id:guid}/counts")]
    [Authorize(Policy = "Inventory.Manage")]
    public async Task<IActionResult> RecordCount(Guid id, [FromBody] RecordCountRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(RecordCount)));
        try
        {
            return Ok(await inventorySessionService.RecordCountAsync(id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(RecordCount), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(RecordCount), ex.Message));
            return BadRequest(new { message = "Impossible d'enregistrer ce comptage." });
        }
    }

    [HttpPost("inventory-sessions/{id:guid}/validate")]
    [Authorize(Policy = "Inventory.Validate")]
    public async Task<IActionResult> ValidateInventorySession(Guid id, [FromBody] ValidateInventorySessionRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(ValidateInventorySession)));
        try
        {
            return Ok(await inventorySessionService.ValidateAsync(id, currentUser!.Id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(ValidateInventorySession), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(ValidateInventorySession), ex.Message));
            return BadRequest(new { message = "Impossible de valider cette session d'inventaire." });
        }
    }

    [HttpPost("inventory-sessions/{id:guid}/recount")]
    [Authorize(Policy = "Inventory.Manage")]
    public async Task<IActionResult> RequestRecount(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(RequestRecount)));
        try
        {
            return Ok(await inventorySessionService.RequestRecountAsync(id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(RequestRecount), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(RequestRecount), ex.Message));
            return BadRequest(new { message = "Impossible de demander un recomptage pour cette session." });
        }
    }

    /// <summary>
    /// Financial masking (constitution §Sécurité): a lot's PRU (UnitCostCfa) is purchase-cost
    /// data, not operational stock data — warehouse-only roles (Magasinier, Préparateur,
    /// Logistique) have Stock.Read to work with quantities/locations/expiry but no business
    /// need to see cost. Admin bypasses via PermissionAuthorizationHandler already.
    /// </summary>
    private bool CanViewCost() => User.HasClaim("permission", "Pricing.Read") || User.HasClaim("permission", "PurchaseOrders.Read") || User.IsInRole("Admin");
}
