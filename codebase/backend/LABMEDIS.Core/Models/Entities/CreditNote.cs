namespace LABMEDIS.Core.Models.Entities;

/// <summary>Credit note (avoir) — generated for every processed return, regardless of disposition (FR-062).</summary>
public class CreditNote : BaseEntity
{
    public string CreditNoteNumber { get; set; } = string.Empty;

    public Guid CustomerReturnId { get; set; }

    public decimal Amount { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public CustomerReturn? CustomerReturn { get; set; }
}
