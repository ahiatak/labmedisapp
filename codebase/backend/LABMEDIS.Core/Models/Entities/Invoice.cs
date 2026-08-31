namespace LABMEDIS.Core.Models.Entities;

public enum InvoiceStatus
{
    Emise = 0,
    Payee = 1,
    EnRetard = 2,
    Annulee = 3
}

/// <summary>Invoice (US7 — FR-058). Each InvoiceLine carries a mandatory StockLotId for BPD traceability.</summary>
public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid SaleOrderId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid CurrencyId { get; set; }

    public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly DueDate { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Emise;

    public decimal TotalHt { get; set; }

    public decimal TotalTva { get; set; }

    public decimal TotalTtc { get; set; }

    public SaleOrder? SaleOrder { get; set; }

    public Customer? Customer { get; set; }

    public Currency? Currency { get; set; }

    public List<InvoiceLine> Lines { get; set; } = [];
}
