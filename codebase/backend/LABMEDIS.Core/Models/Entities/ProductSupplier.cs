namespace LABMEDIS.Core.Models.Entities;

/// <summary>Ordered association between a product and its potential suppliers.</summary>
public class ProductSupplier : BaseEntity
{
    public Guid ProductId { get; set; }

    public Guid SupplierId { get; set; }

    /// <summary>Preference order — lower value is preferred first.</summary>
    public int Priority { get; set; }

    public Product? Product { get; set; }

    public Supplier? Supplier { get; set; }
}
