namespace LABMEDIS.Core.Models.Entities;

/// <summary>Granular permission (FR-015), code format "Module.Action" (e.g. "Products.Create").</summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
