namespace LABMEDIS.Core.Models.Entities;

public class SaleOrderLine : BaseEntity
{
    public Guid SaleOrderId { get; set; }

    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    /// <summary>Set at confirmation — the FEFO-selected (or manually derogated) lot (FR-036/FR-037).</summary>
    public Guid? AllocatedStockLotId { get; set; }

    public decimal UnitPriceHt { get; set; }

    /// <summary>Required when AllocatedStockLotId differs from the first FEFO candidate.</summary>
    public string? DerogationReason { get; set; }

    public SaleOrder? SaleOrder { get; set; }

    public Product? Product { get; set; }

    public StockLot? AllocatedStockLot { get; set; }
}
