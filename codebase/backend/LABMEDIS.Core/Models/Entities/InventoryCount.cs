using System.ComponentModel.DataAnnotations.Schema;

namespace LABMEDIS.Core.Models.Entities;

public class InventoryCount : BaseEntity
{
    public Guid InventorySessionId { get; set; }

    public Guid StockLotId { get; set; }

    public int SystemQuantity { get; set; }

    /// <summary>Null until counted via POST .../counts.</summary>
    public int? CountedQuantity { get; set; }

    /// <summary>Required when the session is validated with a non-zero variance (FR-044).</summary>
    public string? AdjustmentReason { get; set; }

    public InventorySession? InventorySession { get; set; }

    public StockLot? StockLot { get; set; }

    [NotMapped]
    public int? Variance => CountedQuantity.HasValue ? CountedQuantity.Value - SystemQuantity : null;
}
