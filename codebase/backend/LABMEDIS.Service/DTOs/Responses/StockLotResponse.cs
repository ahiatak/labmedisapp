using LABMEDIS.Service.Extensions;
using StockLotEntity = LABMEDIS.Core.Models.Entities.StockLot;
using StockMovementEntity = LABMEDIS.Core.Models.Entities.StockMovement;

namespace LABMEDIS.Service.DTOs.Responses;

public class StockLotResponse
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string? ProductDesignation { get; set; }

    public string SupplierLotNumber { get; set; } = string.Empty;

    public string InternalLotNumber { get; set; } = string.Empty;

    public DateOnly ReceptionDate { get; set; }

    public DateOnly ExpiryDate { get; set; }

    public int InitialQuantity { get; set; }

    public int RemainingQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public int AvailableQuantity { get; set; }

    /// <summary>PRU (cost price) — masked (null) for callers without Pricing.Read/PurchaseOrders.Read (constitution §Sécurité: "le masquage des données financières DOIT s'appliquer selon les permissions de l'utilisateur consultant"). See StockController.MaskCostIfUnauthorized.</summary>
    public string? UnitCostCfa { get; set; } = "0";

    public string QualityStatus { get; set; } = string.Empty;

    public string? QuarantineReason { get; set; }

    public StockLotResponse()
    {
    }

    public StockLotResponse(StockLotEntity entity)
    {
        Id = entity.Id;
        ProductId = entity.ProductId;
        ProductDesignation = entity.Product?.Designation;
        SupplierLotNumber = entity.SupplierLotNumber;
        InternalLotNumber = entity.InternalLotNumber;
        ReceptionDate = entity.ReceptionDate;
        ExpiryDate = entity.ExpiryDate;
        InitialQuantity = entity.InitialQuantity;
        RemainingQuantity = entity.RemainingQuantity;
        ReservedQuantity = entity.ReservedQuantity;
        AvailableQuantity = entity.AvailableQuantity;
        UnitCostCfa = entity.UnitCostCfa.ToInvariantString("0.##");
        QualityStatus = entity.QualityStatus.ToString();
        QuarantineReason = entity.QuarantineReason;
    }
}

public class FefoSuggestionLineResponse
{
    public Guid LotId { get; set; }

    public DateOnly ExpiryDate { get; set; }

    public int QuantityAllocated { get; set; }
}

public class FefoSuggestionResponse
{
    public Guid ProductId { get; set; }

    public int RequestedQuantity { get; set; }

    public List<FefoSuggestionLineResponse> Lines { get; set; } = [];
}

public class AvailableStockResponse
{
    public Guid ProductId { get; set; }

    public int TotalAvailable { get; set; }
}

public class StockMovementResponse
{
    public Guid Id { get; set; }

    public Guid StockLotId { get; set; }

    public string MovementType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Reason { get; set; }

    public StockMovementResponse(StockMovementEntity entity)
    {
        Id = entity.Id;
        StockLotId = entity.StockLotId;
        MovementType = entity.MovementType.ToString();
        Quantity = entity.Quantity;
        CreatedAt = entity.CreatedAt;
        Reason = entity.Reason;
    }
}
