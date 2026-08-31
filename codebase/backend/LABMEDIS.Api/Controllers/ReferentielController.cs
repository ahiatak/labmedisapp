using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Controlled lookup lists (Category/TherapeuticClass/PharmaceuticalForm — US1, FR-003).</summary>
[ApiController]
[Route("api/referentiel")]
[Authorize]
public class ReferentielController(IReferentielService referentielService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet("categories")]
    [Authorize(Policy = "Products.Read")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetCategories)));
        try
        {
            return Ok(await referentielService.GetCategoriesAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetCategories), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les catégories." });
        }
    }

    [HttpPost("categories")]
    [Authorize(Policy = "Products.Create")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateLookupRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(CreateCategory)));
        try
        {
            return Ok(await referentielService.CreateCategoryAsync(request, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateCategory), ex.Message));
            return BadRequest(new { message = "Impossible de créer cette catégorie." });
        }
    }

    [HttpGet("therapeutic-classes")]
    [Authorize(Policy = "Products.Read")]
    public async Task<IActionResult> GetTherapeuticClasses(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetTherapeuticClasses)));
        try
        {
            return Ok(await referentielService.GetTherapeuticClassesAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetTherapeuticClasses), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les classes thérapeutiques." });
        }
    }

    [HttpPost("therapeutic-classes")]
    [Authorize(Policy = "Products.Create")]
    public async Task<IActionResult> CreateTherapeuticClass([FromBody] CreateLookupRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(CreateTherapeuticClass)));
        try
        {
            return Ok(await referentielService.CreateTherapeuticClassAsync(request, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreateTherapeuticClass), ex.Message));
            return BadRequest(new { message = "Impossible de créer cette classe thérapeutique." });
        }
    }

    [HttpGet("pharmaceutical-forms")]
    [Authorize(Policy = "Products.Read")]
    public async Task<IActionResult> GetPharmaceuticalForms(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetPharmaceuticalForms)));
        try
        {
            return Ok(await referentielService.GetPharmaceuticalFormsAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetPharmaceuticalForms), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les formes pharmaceutiques." });
        }
    }

    [HttpPost("pharmaceutical-forms")]
    [Authorize(Policy = "Products.Create")]
    public async Task<IActionResult> CreatePharmaceuticalForm([FromBody] CreateLookupRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(CreatePharmaceuticalForm)));
        try
        {
            return Ok(await referentielService.CreatePharmaceuticalFormAsync(request, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(CreatePharmaceuticalForm), ex.Message));
            return BadRequest(new { message = "Impossible de créer cette forme pharmaceutique." });
        }
    }
}
