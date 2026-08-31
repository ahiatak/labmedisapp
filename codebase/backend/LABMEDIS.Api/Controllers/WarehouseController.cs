using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Warehouse/StorageLocation administration (US4, FR-034) — supports reception (storageLocationId) and the Warehouse page.</summary>
[ApiController]
[Route("api/warehouses")]
[Authorize]
public class WarehouseController(IWarehouseService warehouseService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Stock.Read")]
    public async Task<IActionResult> GetWarehouses(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetWarehouses)));
        try
        {
            return Ok(await warehouseService.GetWarehousesAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetWarehouses), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les entrepôts." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "Stock.Move")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(CreateWarehouse)));
        try
        {
            return Ok(await warehouseService.CreateWarehouseAsync(request, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateWarehouse), ex.Message));
            return BadRequest(new { message = "Impossible de créer cet entrepôt." });
        }
    }

    [HttpGet("locations")]
    [Authorize(Policy = "Stock.Read")]
    public async Task<IActionResult> GetLocations([FromQuery] Guid? warehouseId, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetLocations)));
        try
        {
            return Ok(await warehouseService.GetLocationsAsync(warehouseId, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetLocations), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les emplacements." });
        }
    }

    [HttpPost("locations")]
    [Authorize(Policy = "Stock.Move")]
    public async Task<IActionResult> CreateLocation([FromBody] CreateStorageLocationRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(CreateLocation)));
        try
        {
            return Ok(await warehouseService.CreateLocationAsync(request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateLocation), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateLocation), ex.Message));
            return BadRequest(new { message = "Impossible de créer cet emplacement." });
        }
    }
}
