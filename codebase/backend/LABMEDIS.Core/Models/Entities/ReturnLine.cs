namespace LABMEDIS.Core.Models.Entities;

public enum ReturnDisposition
{
    RemiseEnStock = 0,
    Quarantaine = 1,
    Destruction = 2
}

public class ReturnLine : BaseEntity
{
    public Guid CustomerReturnId { get; set; }

    public Guid SaleOrderLineId { get; set; }

    public Guid? OriginalStockLotId { get; set; }

    public int Quantity { get; set; }

    public ReturnDisposition Disposition { get; set; }

    /// <summary>Required when Disposition = Quarantaine (FR-061).</summary>
    public string? Motif { get; set; }

    public CustomerReturn? CustomerReturn { get; set; }

    public SaleOrderLine? SaleOrderLine { get; set; }

    public StockLot? OriginalStockLot { get; set; }
}
