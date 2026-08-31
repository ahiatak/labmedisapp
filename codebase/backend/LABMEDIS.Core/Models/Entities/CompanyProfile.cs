namespace LABMEDIS.Core.Models.Entities;

public enum CreditLimitEnforcement
{
    Alert = 0,
    Block = 1
}

/// <summary>
/// Singleton configuration entity (a single active row is expected). Holds the values the
/// spec explicitly requires to be admin-configurable rather than hardcoded:
/// the purchase-order validation threshold (FR-023) and the credit-limit enforcement mode
/// (FR-009 — alert vs. block a new sale order once a customer's outstanding balance exceeds
/// their credit limit).
/// </summary>
public class CompanyProfile : BaseEntity
{
    public string CompanyName { get; set; } = "LABMEDIS";

    /// <summary>Amount (CFA) above which a purchase order requires Direction validation (FR-023).</summary>
    public decimal PurchaseOrderValidationThresholdCfa { get; set; }

    /// <summary>Default VAT rate applied when a product does not override it.</summary>
    public decimal DefaultVatRate { get; set; }

    public CreditLimitEnforcement CreditLimitEnforcement { get; set; } = CreditLimitEnforcement.Alert;
}
