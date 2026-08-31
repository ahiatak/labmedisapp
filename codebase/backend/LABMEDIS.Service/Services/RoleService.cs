using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Role administration (US2 — FR-015/FR-016). Like UserService, ApplicationRole is an
/// ASP.NET Core Identity entity managed through RoleManager rather than a custom
/// I[Entité]Repository/BaseRepository — Principle II targets the 59 business tables, not the
/// framework-provided Identity store.
/// </summary>
public class RoleService(RoleManager<ApplicationRole> roleManager, IPermissionService permissionService) : IRoleService
{
    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = roleManager.Roles.Where(r => r.IsActive).ToList();
        var result = new List<RoleResponse>();
        foreach (var role in roles)
        {
            var permissions = await permissionService.GetEffectivePermissionCodesAsync(Guid.Empty, [role.Id], cancellationToken);
            result.Add(new RoleResponse(role, permissions.ToList()));
        }

        return result;
    }

    public async Task<RoleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
        {
            return null;
        }

        var permissions = await permissionService.GetEffectivePermissionCodesAsync(Guid.Empty, [role.Id], cancellationToken);
        return new RoleResponse(role, permissions.ToList());
    }

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (await roleManager.RoleExistsAsync(request.Name))
        {
            throw new AppException(409, "ROLE_NAME_DUPLICATE", "Un rôle avec ce nom existe déjà.");
        }

        var role = new ApplicationRole { Name = request.Name, Description = request.Description, IsSystem = false, IsActive = true };
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new AppException(400, "ROLE_CREATE_FAILED", string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await permissionService.SetRolePermissionsAsync(role.Id, request.PermissionIds, cancellationToken);
        var permissions = await permissionService.GetEffectivePermissionCodesAsync(Guid.Empty, [role.Id], cancellationToken);
        return new RoleResponse(role, permissions.ToList());
    }

    public async Task<RoleResponse> UpdatePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new AppException(404, "ROLE_NOT_FOUND", "Rôle introuvable.");

        await permissionService.SetRolePermissionsAsync(roleId, request.PermissionIds, cancellationToken);
        var permissions = await permissionService.GetEffectivePermissionCodesAsync(Guid.Empty, [roleId], cancellationToken);
        return new RoleResponse(role, permissions.ToList());
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        await permissionService.EnsureSeededAsync(cancellationToken);
        var catalog = await permissionService.GetCatalogAsync(cancellationToken);
        var codeToId = catalog.ToDictionary(p => p.Code, p => p.Id);

        foreach (var (roleName, permissionCodes) in PermissionCatalog.DefaultRolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                role = new ApplicationRole { Name = roleName, IsSystem = true, IsActive = true };
                await roleManager.CreateAsync(role);
            }

            var permissionIds = permissionCodes.Where(codeToId.ContainsKey).Select(code => codeToId[code]);
            await permissionService.SetRolePermissionsAsync(role.Id, permissionIds, cancellationToken);
        }
    }
}
