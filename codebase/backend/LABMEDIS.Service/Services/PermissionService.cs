using LABMEDIS.Core;
using LABMEDIS.Core.Models.Entities;
using LABMEDIS.Core.Repositories.Permission;
using LABMEDIS.Service.DTOs.Responses;
using Microsoft.EntityFrameworkCore;
using PermissionEntity = LABMEDIS.Core.Models.Entities.Permission;

namespace LABMEDIS.Service.Services;

/// <summary>Permission catalogue and effective-permission resolution (US2 — FR-015/FR-016/FR-019).</summary>
public class PermissionService(AppDbContext context) : PermissionRepository(context), IPermissionService
{
    public async Task<IReadOnlyList<PermissionResponse>> GetCatalogAsync(CancellationToken cancellationToken = default) =>
        (await GetAllAsync(cancellationToken)).Select(p => new PermissionResponse(p)).ToList();

    // GetEffectivePermissionCodesAsync and SetRolePermissionsAsync are already implemented by
    // PermissionRepository (inherited) and satisfy IPermissionService directly.

    public async Task<IReadOnlyCollection<string>> GetEffectivePermissionCodesForRoleNamesAsync(
        Guid userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
    {
        var roleNameList = roleNames.ToList();
        var roleIds = await Context.Set<ApplicationRole>()
            .Where(r => roleNameList.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        return await GetEffectivePermissionCodesAsync(userId, roleIds, cancellationToken);
    }

    public async Task SetUserExceptionsAsync(
        Guid userId, IEnumerable<(Guid PermissionId, bool IsGranted, string? Reason)> exceptions, CancellationToken cancellationToken = default)
    {
        var existing = await Context.Set<UserPermissionException>().Where(e => e.UserId == userId).ToListAsync(cancellationToken);
        Context.Set<UserPermissionException>().RemoveRange(existing);

        foreach (var (permissionId, isGranted, reason) in exceptions)
        {
            await Context.Set<UserPermissionException>().AddAsync(new UserPermissionException
            {
                UserId = userId,
                PermissionId = permissionId,
                IsGranted = isGranted,
                Reason = reason
            }, cancellationToken);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        var existingCodes = (await GetAllAsync(cancellationToken)).Select(p => p.Code).ToHashSet();

        foreach (var (code, description) in PermissionCatalog.All)
        {
            if (existingCodes.Contains(code))
            {
                continue;
            }

            var parts = code.Split('.', 2);
            await AddAsync(new PermissionEntity
            {
                Code = code,
                Module = parts[0],
                Action = parts.Length > 1 ? parts[1] : string.Empty,
                Description = description
            }, cancellationToken);
        }
    }
}
