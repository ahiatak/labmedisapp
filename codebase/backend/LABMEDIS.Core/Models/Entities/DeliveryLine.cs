namespace LABMEDIS.Core.Models.Entities;

public class DeliveryLine : BaseEntity
{
    public Guid DeliveryId { get; set; }

    public Guid SaleOrderLineId { get; set; }

    public int QuantityDelivered { get; set; }

    public Delivery? Delivery { get; set; }

    public SaleOrderLine? SaleOrderLine { get; set; }
}
