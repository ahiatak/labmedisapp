namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Negotiated price for a customer/product pair over a validity period (FR-011). No two
/// rows for the same (CustomerId, ProductId) may have overlapping [ValidFrom, ValidTo]
/// periods — enforced in CustomerService, not by a database constraint.
/// </summary>
public class CustomerProductPrice : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Guid ProductId { get; set; }

    public decimal UnitPrice { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly ValidTo { get; set; }

    public Customer? Customer { get; set; }

    public Product? Product { get; set; }
}
