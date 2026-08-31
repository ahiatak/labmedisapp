using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Reporting et tableaux de bord (US11 — contracts/reporting.md, FR-068 à FR-075).</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = "Reports.Read")]
public class ReportsController(IReportingService reportingService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet("dashboard/direction")]
    public async Task<IActionResult> GetDirectionDashboard(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetDirectionDashboard)));
        try
        {
            return Ok(await reportingService.GetDirectionDashboardAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetDirectionDashboard), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer le tableau de bord Direction." });
        }
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStockReport(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetStockReport)));
        try
        {
            return Ok(await reportingService.GetStockReportAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetStockReport), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer le rapport de stock." });
        }
    }

    [HttpGet("lots/expiring")]
    public async Task<IActionResult> GetExpiringLots([FromQuery] int days, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetExpiringLots)));
        try
        {
            return Ok(await reportingService.GetExpiringLotsAsync(days <= 0 ? 90 : days, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetExpiringLots), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les lots proches de péremption." });
        }
    }

    [HttpGet("lots/slow-moving")]
    public async Task<IActionResult> GetSlowMovingLots(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetSlowMovingLots)));
        try
        {
            return Ok(await reportingService.GetSlowMovingProductsAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetSlowMovingLots), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les produits à rotation lente." });
        }
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetSalesReport)));
        try
        {
            return Ok(await reportingService.GetSalesReportAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetSalesReport), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer le rapport des ventes." });
        }
    }

    [HttpGet("pricing")]
    public async Task<IActionResult> GetPricingReport(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetPricingReport)));
        try
        {
            return Ok(await reportingService.GetPricingReportAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetPricingReport), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer le rapport de pricing." });
        }
    }

    [HttpGet("quality")]
    public async Task<IActionResult> GetQualityReport(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetQualityReport)));
        try
        {
            return Ok(await reportingService.GetQualityReportAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetQualityReport), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer le rapport qualité." });
        }
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] ExportReportRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Export)));
        try
        {
            var (content, contentType, fileName) = await reportingService.ExportAsync(request, cancellationToken);
            return File(content, contentType, fileName);
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Export), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Export), ex.Message));
            return BadRequest(new { message = "L'export du rapport a échoué." });
        }
    }
}
