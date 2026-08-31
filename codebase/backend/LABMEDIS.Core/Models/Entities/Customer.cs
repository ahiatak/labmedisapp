namespace LABMEDIS.Core.Models.Entities;

public enum CustomerType
{
    Repartiteur = 0,
    Hopital = 1,
    Clinique = 2,
    Pharmacie = 3,
    CentraleAchat = 4,
    Autre = 5
}

/// <summary>Customer (US1 — FR-008 à FR-011). Name is unique among active customers only.</summary>
public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public CustomerType Type { get; set; }

    public string? Address { get; set; }

    public int PaymentDays { get; set; } = 30;

    /// <summary>Outstanding-balance ceiling (FR-009). Null means no limit is enforced.</summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>When false, no new sale order may be created for this customer (FR-010).</summary>
    public bool IsActive { get; set; } = true;

    public List<CustomerProductPrice> NegotiatedPrices { get; set; } = [];
}
