namespace LABMEDIS.Service.DTOs.Requests;

public class CreatePurchaseOrderLineRequest
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }

    public int? CartonQuantity { get; set; }

    /// <summary>Unit price in the order's foreign currency — STRING obligatoire (Principe VI).</summary>
    public string UnitPriceForeign { get; set; } = "0";

    /// <summary>Id of the product's ProductPackaging row (the contract calls this "packagingTypeId").</summary>
    public Guid PackagingId { get; set; }
}

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }

    public Guid CurrencyId { get; set; }

    public string TransportMode { get; set; } = "Maritime";

    public string? Incoterm { get; set; }

    public List<CreatePurchaseOrderLineRequest> Lines { get; set; } = [];
}

public class CancelPurchaseOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}
