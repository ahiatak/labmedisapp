using InventoryCountEntity = LABMEDIS.Core.Models.Entities.InventoryCount;
using InventorySessionEntity = LABMEDIS.Core.Models.Entities.InventorySession;

namespace LABMEDIS.Service.DTOs.Responses;

public class InventoryCountResponse
{
    public Guid Id { get; set; }

    public Guid StockLotId { get; set; }

    public string? InternalLotNumber { get; set; }

    public string? ProductDesignation { get; set; }

    public int SystemQuantity { get; set; }

    public int? CountedQuantity { get; set; }

    public int? Variance { get; set; }

    public string? AdjustmentReason { get; set; }

    public InventoryCountResponse(InventoryCountEntity entity)
    {
        Id = entity.Id;
        StockLotId = entity.StockLotId;
        InternalLotNumber = entity.StockLot?.InternalLotNumber;
        ProductDesignation = entity.StockLot?.Product?.Designation;
        SystemQuantity = entity.SystemQuantity;
        CountedQuantity = entity.CountedQuantity;
        Variance = entity.Variance;
        AdjustmentReason = entity.AdjustmentReason;
    }
}

public class InventorySessionResponse
{
    public Guid Id { get; set; }

    public string SessionNumber { get; set; } = string.Empty;

    public string Perimeter { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? FrozenAt { get; set; }

    public List<InventoryCountResponse> Counts { get; set; } = [];

    public InventorySessionResponse(InventorySessionEntity entity)
    {
        Id = entity.Id;
        SessionNumber = entity.SessionNumber;
        Perimeter = entity.Perimeter;
        Status = entity.Status.ToString();
        FrozenAt = entity.FrozenAt;
        Counts = entity.Counts.Select(c => new InventoryCountResponse(c)).ToList();
    }
}
