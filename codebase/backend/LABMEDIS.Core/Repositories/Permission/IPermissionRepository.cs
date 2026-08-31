using LABMEDIS.Core.Repositories.Base;
using PermissionEntity = LABMEDIS.Core.Models.Entities.Permission;

namespace LABMEDIS.Core.Repositories.Permission;

public interface IPermissionRepository : IBaseRepository<PermissionEntity>
{
    Task<PermissionEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionEntity>> GetByRoleIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Models.Entities.UserPermissionException>> GetUserExceptionsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>Resolves the effective permission codes for a user: role-derived permissions, adjusted by per-user exceptions (FR-016/FR-019).</summary>
    Task<IReadOnlyCollection<string>> GetEffectivePermissionCodesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);
}
