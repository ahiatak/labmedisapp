namespace LABMEDIS.Core.Models.Entities;

/// <summary>Historized manufacture/transport lead time for a (product, supplier) couple (US10).</summary>
public class SupplierLeadTime : BaseEntity
{
    public Guid ProductId { get; set; }

    public Guid SupplierId { get; set; }

    public int ManufactureDays { get; set; }

    public int TransportDays { get; set; }

    public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public Product? Product { get; set; }

    public Supplier? Supplier { get; set; }
}
