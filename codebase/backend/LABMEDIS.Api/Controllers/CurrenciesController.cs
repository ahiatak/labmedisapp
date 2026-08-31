using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Supported currencies (EUR, USD, XOF — FR-085), used to populate supplier/pricing forms.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CurrenciesController(ICurrencyService currencyService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAll)));
        try
        {
            return Ok(await currencyService.ListAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAll), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les devises." });
        }
    }
}
