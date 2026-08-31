namespace LABMEDIS.Core.Models.Entities;

/// <summary>Delivery note (BL) — distinct from the invoice (FR-057).</summary>
public class Delivery : BaseEntity
{
    public Guid SaleOrderId { get; set; }

    public DateOnly DeliveryDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public SaleOrder? SaleOrder { get; set; }

    public List<DeliveryLine> Lines { get; set; } = [];
}
