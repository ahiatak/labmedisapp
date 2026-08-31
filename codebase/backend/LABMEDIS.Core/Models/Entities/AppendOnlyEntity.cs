namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Base class for append-only traceability rows (LoginAudit, StockMovement,
/// PurchaseOrderStatusHistory, NotificationRead, AuditLog — data-model.md §"Soft delete
/// exclusif"). These rows are never updated nor soft-deleted after creation, so they carry
/// only Id/CreatedAt rather than the full BaseEntity shape.
/// </summary>
public abstract class AppendOnlyEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
