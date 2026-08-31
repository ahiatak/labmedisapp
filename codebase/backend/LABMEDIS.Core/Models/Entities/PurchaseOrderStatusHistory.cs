namespace LABMEDIS.Core.Models.Entities;

/// <summary>Append-only transition log (FR-022) — never updated nor soft-deleted after creation.</summary>
public class PurchaseOrderStatusHistory : AppendOnlyEntity
{
    public Guid PurchaseOrderId { get; set; }

    public PurchaseOrderStatus FromStatus { get; set; }

    public PurchaseOrderStatus ToStatus { get; set; }

    public Guid ChangedByUserId { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
