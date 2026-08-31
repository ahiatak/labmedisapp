using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Product catalogue (US1 — contracts/products-referentiel.md). Each action requires its documented permission (FR-015).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(IProductService productService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Products.Read")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] bool selectableOnly = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAll)));
        try
        {
            var (items, totalCount) = await productService.ListAsync(search, selectableOnly, page, pageSize, cancellationToken);
            return Ok(new { items, totalCount, page, pageSize });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAll), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer la liste des produits." });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Products.Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetById)));
        try
        {
            var product = await productService.GetAsync(id, cancellationToken);
            return product is null ? NotFound(new { message = "Produit introuvable." }) : Ok(product);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetById), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer ce produit." });
        }
    }

    [HttpGet("{id:guid}/stock")]
    [Authorize(Policy = "Products.Read")]
    public async Task<IActionResult> GetStock(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetStock)));
        try
        {
            var product = await productService.GetAsync(id, cancellationToken);
            if (product is null)
            {
                return NotFound(new { message = "Produit introuvable." });
            }

            // Aggregated available quantity across lots is wired once StockLot exists (US4).
            return Ok(new { productId = id, totalAvailable = 0 });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetStock), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer le stock de ce produit." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "Products.Create")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Create)));
        try
        {
            var created = await productService.CreateAsync(request, cancellationToken);
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
            return BadRequest(new { message = "Impossible de créer ce produit." });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Products.Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Update)));
        try
        {
            var updated = await productService.UpdateAsync(id, request, cancellationToken);
            return Ok(updated);
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Update), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Update), ex.Message));
            return BadRequest(new { message = "Impossible de mettre à jour ce produit." });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Products.Delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Delete)));
        try
        {
            await productService.DeactivateAsync(id, cancellationToken);
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
            return BadRequest(new { message = "Impossible de désactiver ce produit." });
        }
    }

    [HttpPost("{id:guid}/packagings")]
    [Authorize(Policy = "Products.Update")]
    public async Task<IActionResult> AddPackaging(Guid id, [FromBody] CreateProductPackagingRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(AddPackaging)));
        try
        {
            return Ok(await productService.AddPackagingAsync(id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddPackaging), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddPackaging), ex.Message));
            return BadRequest(new { message = "Impossible d'ajouter ce conditionnement." });
        }
    }

    [HttpPost("import")]
    [Authorize(Policy = "Products.Create")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Import)));
        try
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest(new { message = "Aucun fichier fourni." });
            }

            await using var stream = file.OpenReadStream();
            var report = await productService.ImportAsync(stream, cancellationToken);
            return Ok(report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Import), ex.Message));
            return BadRequest(new { message = "L'import du catalogue a échoué." });
        }
    }
}
