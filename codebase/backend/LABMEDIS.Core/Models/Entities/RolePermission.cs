namespace LABMEDIS.Core.Models.Entities;

/// <summary>Association: a role grants a permission (FR-015/FR-016).</summary>
public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }

    public ApplicationRole? Role { get; set; }

    public Guid PermissionId { get; set; }

    public Permission? Permission { get; set; }
}
