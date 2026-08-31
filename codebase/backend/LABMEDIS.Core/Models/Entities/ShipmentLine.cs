namespace LABMEDIS.Core.Models.Entities;

public class ShipmentLine : BaseEntity
{
    public Guid ShipmentId { get; set; }

    public Guid PurchaseOrderLineId { get; set; }

    public Shipment? Shipment { get; set; }

    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
}
