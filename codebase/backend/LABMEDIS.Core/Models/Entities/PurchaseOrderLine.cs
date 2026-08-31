namespace LABMEDIS.Core.Models.Entities;

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public int? CartonQuantity { get; set; }

    /// <summary>Unit price in the order's foreign currency (before conversion via the locked exchange rate).</summary>
    public decimal UnitPriceForeign { get; set; }

    public Guid PackagingId { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public Product? Product { get; set; }

    public ProductPackaging? Packaging { get; set; }
}
