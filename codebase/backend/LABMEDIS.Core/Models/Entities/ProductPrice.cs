namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Immutable price history row (FR-050) — every change is a NEW row, never an UPDATE.
/// PriceGap = PvHtCalculated - PvHtApplied is preserved forever, even after a manual
/// adjustment (FR-049): it is never overwritten or reset to zero automatically.
/// </summary>
public class ProductPrice : BaseEntity
{
    public Guid ProductId { get; set; }

    /// <summary>PMP/CUMP at the time this price was computed.</summary>
    public decimal CumpCfa { get; set; }

    public decimal PvHtCalculated { get; set; }

    public decimal PvHtApplied { get; set; }

    public decimal PriceGap { get; set; }

    public decimal VatRate { get; set; }

    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserId { get; set; }

    public Product? Product { get; set; }
}
