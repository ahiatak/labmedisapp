namespace LABMEDIS.Service.DTOs.Requests;

public class CreateSaleOrderLineRequest
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
}

public class CreateSaleOrderRequest
{
    public Guid CustomerId { get; set; }

    public Guid CurrencyId { get; set; }

    public List<CreateSaleOrderLineRequest> Lines { get; set; } = [];
}
