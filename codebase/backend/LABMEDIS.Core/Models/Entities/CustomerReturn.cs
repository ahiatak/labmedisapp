namespace LABMEDIS.Core.Models.Entities;

public enum CustomerReturnStatus
{
    Initie = 0,
    Traite = 1
}

/// <summary>Customer return (US8 — FR-060 à FR-062), attached to a delivered sale order.</summary>
public class CustomerReturn : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;

    public Guid SaleOrderId { get; set; }

    public Guid CustomerId { get; set; }

    public DateOnly ReturnDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public CustomerReturnStatus Status { get; set; } = CustomerReturnStatus.Initie;

    public string Reason { get; set; } = string.Empty;

    public Guid? CreditNoteId { get; set; }

    public SaleOrder? SaleOrder { get; set; }

    public Customer? Customer { get; set; }

    public List<ReturnLine> Lines { get; set; } = [];
}
