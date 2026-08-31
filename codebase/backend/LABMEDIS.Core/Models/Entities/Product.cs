namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Product catalogue entry (US1 — FR-001 à FR-011). Designation and CodeCip are unique
/// among active products only (partial unique index WHERE deleted_at IS NULL, Principle III).
/// </summary>
public class Product : BaseEntity
{
    public string Designation { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Guid? TherapeuticClassId { get; set; }

    public Guid? PharmaceuticalFormId { get; set; }

    public string? Dosage { get; set; }

    public string? CodeCip { get; set; }

    public TransportMode? DefaultTransportMode { get; set; }

    /// <summary>Manufacturing lead time in days — feeds MRP (FR-063).</summary>
    public int? ManufactureLeadDays { get; set; }

    /// <summary>Delivery lead time in days — feeds MRP.</summary>
    public int? DeliveryLeadDays { get; set; }

    public int SafetyStockQty { get; set; }

    /// <summary>VAT rate, 0.0000-0.9999 (FR-087). Medicines are 0% per UEMOA directive 06/2002.</summary>
    public decimal VatRate { get; set; }

    public bool IsTaxable { get; set; } = true;

    /// <summary>When false, the product is excluded from selection lists (FR-005) but stays visible in history.</summary>
    public bool IsActive { get; set; } = true;

    public Category? Category { get; set; }

    public TherapeuticClass? TherapeuticClass { get; set; }

    public PharmaceuticalForm? PharmaceuticalFormEntity { get; set; }

    public List<ProductPackaging> Packagings { get; set; } = [];

    public List<ProductSupplier> ProductSuppliers { get; set; } = [];
}
