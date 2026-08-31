namespace LABMEDIS.Core.Models.Entities;

/// <summary>State machine of FR-056: Brouillon → Confirmée → Livrée → Facturée, or [Brouillon|Confirmée] → Annulée.</summary>
public enum SaleOrderStatus
{
    Brouillon = 0,
    Confirmee = 1,
    Livree = 2,
    Facturee = 3,
    Annulee = 4
}

/// <summary>Sale order (US7 — FR-054 à FR-059).</summary>
public class SaleOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }

    public Guid CurrencyId { get; set; }

    public SaleOrderStatus Status { get; set; } = SaleOrderStatus.Brouillon;

    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public decimal TotalHt { get; set; }

    public decimal TotalTva { get; set; }

    public decimal TotalTtc { get; set; }

    public Guid CreatedByUserId { get; set; }

    public Customer? Customer { get; set; }

    public Currency? Currency { get; set; }

    public List<SaleOrderLine> Lines { get; set; } = [];
}
