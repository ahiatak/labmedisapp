using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionResponse>> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetEffectivePermissionCodesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>Same resolution as <see cref="GetEffectivePermissionCodesAsync"/> but starting from role names (as returned by UserManager.GetRolesAsync).</summary>
    Task<IReadOnlyCollection<string>> GetEffectivePermissionCodesForRoleNamesAsync(Guid userId, IEnumerable<string> roleNames, CancellationToken cancellationToken = default);

    Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

    Task SetUserExceptionsAsync(Guid userId, IEnumerable<(Guid PermissionId, bool IsGranted, string? Reason)> exceptions, CancellationToken cancellationToken = default);

    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
}
