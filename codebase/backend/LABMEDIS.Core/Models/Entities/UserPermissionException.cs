namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Per-user exception overriding the role-derived permission set (FR-016/FR-019):
/// IsGranted = true adds a permission the user's role does not carry, IsGranted = false
/// revokes one the role would otherwise carry.
/// </summary>
public class UserPermissionException : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public Permission? Permission { get; set; }

    public bool IsGranted { get; set; }

    public string? Reason { get; set; }
}
