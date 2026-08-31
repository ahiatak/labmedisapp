using ApplicationRoleEntity = LABMEDIS.Core.Models.Entities.ApplicationRole;
using PermissionEntity = LABMEDIS.Core.Models.Entities.Permission;

namespace LABMEDIS.Service.DTOs.Responses;

public class RoleResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public bool IsActive { get; set; }

    public IReadOnlyList<string> Permissions { get; set; } = [];

    public RoleResponse()
    {
    }

    public RoleResponse(ApplicationRoleEntity entity, IReadOnlyList<string>? permissions = null)
    {
        Id = entity.Id;
        Name = entity.Name ?? string.Empty;
        Description = entity.Description;
        IsSystem = entity.IsSystem;
        IsActive = entity.IsActive;
        Permissions = permissions ?? [];
    }
}

public class PermissionResponse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? Description { get; set; }

    public PermissionResponse()
    {
    }

    public PermissionResponse(PermissionEntity entity)
    {
        Id = entity.Id;
        Code = entity.Code;
        Module = entity.Module;
        Action = entity.Action;
        Description = entity.Description;
    }
}
