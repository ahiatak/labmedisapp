namespace LABMEDIS.Core.Models.Entities;

public enum PackagingType
{
    Unite = 0,
    Carton = 1,
    Palette = 2,
    ColisExpress = 3
}

/// <summary>Packaging unit conversion for a product (e.g. 1 carton = 100 units).</summary>
public class ProductPackaging : BaseEntity
{
    public Guid ProductId { get; set; }

    public PackagingType PackagingType { get; set; }

    public int QuantityPerPackage { get; set; }

    public Product? Product { get; set; }
}
