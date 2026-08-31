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
public class CustomersController(ICustomerService customerService, ILoggerManager logger, IUserService userService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Customers.Read")]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAll)));
        try
        {
            return Ok(await customerService.ListAsync(search, activeOnly, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAll), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer la liste des clients." });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Customers.Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetById)));
        try
        {
            var customer = await customerService.GetAsync(id, cancellationToken);
            return customer is null ? NotFound(new { message = "Client introuvable." }) : Ok(customer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetById), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer ce client." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "Customers.Create")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Create)));
        try
        {
            var created = await customerService.CreateAsync(request, cancellationToken);
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
            return BadRequest(new { message = "Impossible de créer ce client." });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Customers.Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Update)));
        try
        {
            return Ok(await customerService.UpdateAsync(id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Update), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Update), ex.Message));
            return BadRequest(new { message = "Impossible de mettre à jour ce client." });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Customers.Delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Delete)));
        try
        {
            await customerService.DeactivateAsync(id, cancellationToken);
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
            return BadRequest(new { message = "Impossible de désactiver ce client." });
        }
    }

    [HttpGet("{id:guid}/outstanding-balance")]
    [Authorize(Policy = "Customers.Read")]
    public async Task<IActionResult> GetOutstandingBalance(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetOutstandingBalance)));
        try
        {
            return Ok(await customerService.GetOutstandingBalanceAsync(id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetOutstandingBalance), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetOutstandingBalance), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer l'encours de ce client." });
        }
    }

    [HttpGet("{id:guid}/negotiated-prices")]
    [Authorize(Policy = "Customers.Read")]
    public async Task<IActionResult> GetNegotiatedPrices(Guid id, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetNegotiatedPrices)));
        try
        {
            return Ok(await customerService.GetNegotiatedPricesAsync(id, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetNegotiatedPrices), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetNegotiatedPrices), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer les tarifs négociés." });
        }
    }

    [HttpPut("{id:guid}/negotiated-prices")]
    [Authorize(Policy = "Customers.Update")]
    public async Task<IActionResult> AddNegotiatedPrice(Guid id, [FromBody] NegotiatedPriceRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(AddNegotiatedPrice)));
        try
        {
            return Ok(await customerService.AddNegotiatedPriceAsync(id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddNegotiatedPrice), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(AddNegotiatedPrice), ex.Message));
            return BadRequest(new { message = "Impossible d'enregistrer ce tarif négocié." });
        }
    }
}
