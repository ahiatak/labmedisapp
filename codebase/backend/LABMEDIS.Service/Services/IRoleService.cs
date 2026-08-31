using LABMEDIS.Service.DTOs.Requests;
using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RoleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<RoleResponse> UpdatePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Seeds the 10 built-in LABMEDIS roles (FR-015) and their default permissions on first startup.</summary>
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
}
