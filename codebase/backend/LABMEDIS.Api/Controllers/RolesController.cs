using LABMEDIS.Api.Extensions;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.Exceptions;
using LABMEDIS.Service.Logging;
using LABMEDIS.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LABMEDIS.Api.Controllers;

/// <summary>Role/permission administration (US2 — FR-015/FR-016).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(IRoleService roleService, IPermissionService permissionService, IUserService userService, ILoggerManager logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "Roles.Read")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetAll)));
        try
        {
            return Ok(await roleService.GetAllAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetAll), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer la liste des rôles." });
        }
    }

    [HttpGet("permissions")]
    [Authorize(Policy = "Roles.Read")]
    public async Task<IActionResult> GetPermissionCatalog(CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(GetPermissionCatalog)));
        try
        {
            return Ok(await permissionService.GetCatalogAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(GetPermissionCatalog), ex.Message));
            return BadRequest(new { message = "Impossible de récupérer le catalogue des permissions." });
        }
    }

    [HttpPost]
    [Authorize(Policy = "Roles.Update")]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(Create)));
        try
        {
            var created = await roleService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAll), created);
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Create), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(Create), ex.Message));
            return BadRequest(new { message = "Impossible de créer ce rôle." });
        }
    }

    [HttpPut("{id:guid}/permissions")]
    [Authorize(Policy = "Roles.Update")]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await userService.GetCurrentUserAsync(User, cancellationToken);
        logger.LogInfo(HttpContext.BuildStartLog(currentUser, nameof(UpdatePermissions)));
        try
        {
            return Ok(await roleService.UpdatePermissionsAsync(id, request, cancellationToken));
        }
        catch (AppException ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(UpdatePermissions), ex.Message));
            return StatusCode(ex.StatusCode, new { message = ex.Message, code = ex.ErrorCode });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, HttpContext.BuildErrorLog(currentUser, nameof(UpdatePermissions), ex.Message));
            return BadRequest(new { message = "Impossible de mettre à jour les permissions de ce rôle." });
        }
    }
}
