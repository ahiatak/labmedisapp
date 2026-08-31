namespace LABMEDIS.Core.Models.Entities;

/// <summary>Supplier (US1 — FR-007). Name is unique among active suppliers only.</summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public Guid DefaultCurrencyId { get; set; }

    /// <summary>Average manufacturing lead time in days — feeds MRP (FR-063).</summary>
    public int? AvgManufactureDays { get; set; }

    /// <summary>Average delivery lead time in days — feeds MRP.</summary>
    public int? AvgDeliveryDays { get; set; }

    public bool IsActive { get; set; } = true;

    public Currency? DefaultCurrency { get; set; }

    public List<ProductSupplier> ProductSuppliers { get; set; } = [];
}
