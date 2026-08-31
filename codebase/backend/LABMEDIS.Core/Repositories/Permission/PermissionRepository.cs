using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using PermissionEntity = LABMEDIS.Core.Models.Entities.Permission;
using RolePermissionEntity = LABMEDIS.Core.Models.Entities.RolePermission;
using UserPermissionExceptionEntity = LABMEDIS.Core.Models.Entities.UserPermissionException;

namespace LABMEDIS.Core.Repositories.Permission;

public class PermissionRepository(AppDbContext context) : BaseRepository<PermissionEntity>(context), IPermissionRepository
{
    public Task<PermissionEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);

    public async Task<IReadOnlyList<PermissionEntity>> GetByRoleIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var roleIdList = roleIds.ToList();
        return await Context.Set<RolePermissionEntity>()
            .Where(rp => roleIdList.Contains(rp.RoleId))
            .Select(rp => rp.Permission!)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserPermissionExceptionEntity>> GetUserExceptionsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Context.Set<UserPermissionExceptionEntity>()
            .Include(e => e.Permission)
            .Where(e => e.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
    {
        var existing = await Context.Set<RolePermissionEntity>().Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
        Context.Set<RolePermissionEntity>().RemoveRange(existing);

        foreach (var permissionId in permissionIds.Distinct())
        {
            await Context.Set<RolePermissionEntity>().AddAsync(new RolePermissionEntity { RoleId = roleId, PermissionId = permissionId }, cancellationToken);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetEffectivePermissionCodesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var rolePermissions = await GetByRoleIdsAsync(roleIds, cancellationToken);
        var codes = rolePermissions.Select(p => p.Code).ToHashSet();

        var exceptions = await GetUserExceptionsAsync(userId, cancellationToken);
        foreach (var exception in exceptions)
        {
            if (exception.Permission is null)
            {
                continue;
            }

            if (exception.IsGranted)
            {
                codes.Add(exception.Permission.Code);
            }
            else
            {
                codes.Remove(exception.Permission.Code);
            }
        }

        return codes;
    }
}
