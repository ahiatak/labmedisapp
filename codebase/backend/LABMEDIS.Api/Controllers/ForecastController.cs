using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Prévision (MRP) et réapprovisionnement (US10 — contracts/forecast.md, FR-063 à FR-067).</summary>
[ApiController]
[Route("api/forecast")]
[Authorize]
public class ForecastController(IForecastService forecastService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet("suggestions")]
    [Authorize(Policy = "Forecast.Read")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetSuggestions)));
        try
        {
            return Ok(await forecastService.GetSuggestionsAsync(status, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetSuggestions), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les suggestions de réapprovisionnement." });
        }
    }

    [HttpPost("suggestions/{id:guid}/convert")]
    [Authorize(Policy = "Forecast.Convert")]
    public async Task<IActionResult> Convert(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Convert)));
        try
        {
            return Ok(await forecastService.ConvertAsync(id, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Convert), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Convert), ex.Message));
            return BadRequest(new { message = "Impossible de convertir cette suggestion." });
        }
    }

    [HttpPost("suggestions/{id:guid}/reject")]
    [Authorize(Policy = "Forecast.Convert")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Reject)));
        try
        {
            await forecastService.RejectAsync(id, cancellationToken);
            return NoContent();
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Reject), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Reject), ex.Message));
            return BadRequest(new { message = "Impossible de rejeter cette suggestion." });
        }
    }

    [HttpPost("run")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Run)));
        try
        {
            var createdCount = await forecastService.RunCalculationAsync(cancellationToken);
            return Ok(new { createdCount });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Run), ex.Message));
            return BadRequest(new { message = "Le calcul de prévision a échoué." });
        }
    }

    [HttpGet("products/{id:guid}/parameters")]
    [Authorize(Policy = "Forecast.Read")]
    public async Task<IActionResult> GetParameters(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetParameters)));
        try
        {
            return Ok(await forecastService.GetParametersAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetParameters), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les paramètres MRP de ce produit." });
        }
    }

    [HttpPut("products/{id:guid}/parameters")]
    [Authorize(Policy = "Forecast.Convert")]
    public async Task<IActionResult> UpdateParameters(Guid id, [FromBody] ForecastParametersRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(UpdateParameters)));
        try
        {
            return Ok(await forecastService.UpdateParametersAsync(id, request, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(UpdateParameters), ex.Message));
            return BadRequest(new { message = "Impossible de mettre à jour les paramètres MRP de ce produit." });
        }
    }
}
