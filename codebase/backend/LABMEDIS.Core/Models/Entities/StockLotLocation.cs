namespace LABMEDIS.Core.Models.Entities;

/// <summary>Splits a StockLot's physical presence across one or more storage locations (FR-039).</summary>
public class StockLotLocation : BaseEntity
{
    public Guid StockLotId { get; set; }

    public Guid StorageLocationId { get; set; }

    public int Quantity { get; set; }

    public int ReservedQuantity { get; set; }

    public StockLot? StockLot { get; set; }

    public StorageLocation? StorageLocation { get; set; }
}
