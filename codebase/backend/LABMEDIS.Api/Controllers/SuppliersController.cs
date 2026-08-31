using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController(ISupplierService supplierService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Suppliers.Read")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAll)));
        try
        {
            return Ok(await supplierService.ListAsync(search, activeOnly, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAll), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer la liste des fournisseurs." });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Suppliers.Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetById)));
        try
        {
            var supplier = await supplierService.GetAsync(id, cancellationToken);
            return supplier is null ? NotFound(new { message = "Fournisseur introuvable." }) : Ok(supplier);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetById), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer ce fournisseur." });
        }
    }

    [HttpGet("{id:guid}/purchase-history")]
    [Authorize(Policy = "Suppliers.Read")]
    public async Task<IActionResult> GetPurchaseHistory(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetPurchaseHistory)));
        try
        {
            var supplier = await supplierService.GetAsync(id, cancellationToken);
            if (supplier is null)
            {
                return NotFound(new { message = "Fournisseur introuvable." });
            }

            // Wired to PurchaseOrderRepository once purchase orders exist (US3).
            return Ok(Array.Empty<object>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetPurchaseHistory), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer l'historique d'achats." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "Suppliers.Create")]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Create)));
        try
        {
            var created = await supplierService.CreateAsync(request, cancellationToken);
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
            return BadRequest(new { message = "Impossible de créer ce fournisseur." });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Suppliers.Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Update)));
        try
        {
            return Ok(await supplierService.UpdateAsync(id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Update), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Update), ex.Message));
            return BadRequest(new { message = "Impossible de mettre à jour ce fournisseur." });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Suppliers.Delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Delete)));
        try
        {
            await supplierService.DeactivateAsync(id, cancellationToken);
            return NoContent();
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Delete), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Delete), ex.Message));
            return BadRequest(new { message = "Impossible de désactiver ce fournisseur." });
        }
    }
}
