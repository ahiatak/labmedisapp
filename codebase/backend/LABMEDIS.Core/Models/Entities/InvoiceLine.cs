namespace LABMEDIS.Core.Models.Entities;

/// <summary>StockLotId is mandatory (never null) — BPD traceability requires every invoiced unit to reference its exact lot (FR-058).</summary>
public class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }

    public Guid ProductId { get; set; }

    public Guid StockLotId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPriceHt { get; set; }

    public decimal VatRate { get; set; }

    public Invoice? Invoice { get; set; }

    public Product? Product { get; set; }

    public StockLot? StockLot { get; set; }
}
