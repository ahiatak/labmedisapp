using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Tarification (US6 — contracts/pricing.md, FR-045 à FR-053).</summary>
[ApiController]
[Route("api/pricing")]
[Authorize]
public class PricingController(IPricingService pricingService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpPost("simulate")]
    [Authorize(Policy = "Pricing.Read")]
    public async Task<IActionResult> Simulate([FromBody] SimulatePricingRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Simulate)));
        try
        {
            return Ok(await pricingService.SimulateAsync(request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Simulate), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Simulate), ex.Message));
            return BadRequest(new { message = "La simulation de prix a échoué." });
        }
    }

    [HttpGet("profiles")]
    [Authorize(Policy = "Pricing.Read")]
    public async Task<IActionResult> GetProfiles(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetProfiles)));
        try
        {
            return Ok(await pricingService.GetProfilesAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetProfiles), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les profils de pricing." });
        }
    }

    [HttpPost("profiles")]
    [Authorize(Policy = "Pricing.Update")]
    public async Task<IActionResult> CreateProfile([FromBody] CreatePricingProfileRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(CreateProfile)));
        try
        {
            return Ok(await pricingService.CreateProfileAsync(request, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateProfile), ex.Message));
            return BadRequest(new { message = "Impossible de créer ce profil de pricing." });
        }
    }

    [HttpPut("profiles/{id:guid}")]
    [Authorize(Policy = "Pricing.Update")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] CreatePricingProfileRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(UpdateProfile)));
        try
        {
            return Ok(await pricingService.UpdateProfileAsync(id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(UpdateProfile), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(UpdateProfile), ex.Message));
            return BadRequest(new { message = "Impossible de mettre à jour ce profil de pricing." });
        }
    }

    [HttpPut("products/{id:guid}/apply-price")]
    [Authorize(Policy = "Pricing.Update")]
    public async Task<IActionResult> ApplyPrice(Guid id, [FromBody] ApplyPriceRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(ApplyPrice)));
        try
        {
            return Ok(await pricingService.ApplyPriceAsync(id, request, currentUser!.Id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(ApplyPrice), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(ApplyPrice), ex.Message));
            return BadRequest(new { message = "Impossible d'appliquer ce prix." });
        }
    }

    [HttpGet("products/{id:guid}/history")]
    [Authorize(Policy = "Pricing.Read")]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetHistory)));
        try
        {
            return Ok(await pricingService.GetHistoryAsync(id, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetHistory), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer l'historique des prix." });
        }
    }
}
