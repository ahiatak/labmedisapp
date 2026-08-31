namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Cascade coefficients used to compute a product's cost/sale price (US6 — FR-045 à FR-053).
/// Never hard-coded: resolved by (SupplierId?, CategoryId?, TransportMode), falling back to
/// the global profile (both null) when no specific match exists (FR-047).
/// </summary>
public class PricingProfile : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid? SupplierId { get; set; }

    public Guid? CategoryId { get; set; }

    public TransportMode TransportMode { get; set; }

    public decimal CommissionCoeff { get; set; } = 1m;

    public decimal FreightCoeff { get; set; } = 1m;

    public decimal TransitCoeff { get; set; } = 1m;

    public decimal TransferFeeCoeff { get; set; } = 1m;

    public decimal TargetMarginCoeff { get; set; } = 1m;

    public bool IsActive { get; set; } = true;
}
