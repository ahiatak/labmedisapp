namespace LABMEDIS.Service.DTOs.Requests;

public class ReceiveLotLineRequest
{
    public Guid LineId { get; set; }

    public string LotNumber { get; set; } = string.Empty;

    public DateOnly ExpiryDate { get; set; }

    public DateOnly? ManufacturingDate { get; set; }

    public int QuantityReceived { get; set; }

    public int? CartonsReceived { get; set; }

    public Guid StorageLocationId { get; set; }

    /// <summary>"EnRéception" or "EnQuarantaine" (contracts/purchase-orders.md).</summary>
    public string QualityStatus { get; set; } = "EnReception";
}

public class RecordMovementRequest
{
    public Guid StockLotId { get; set; }

    public string MovementType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public Guid? SourceLocationId { get; set; }

    public Guid? DestinationLocationId { get; set; }

    /// <summary>Required for AjustementPositif/AjustementNegatif (FR-038).</summary>
    public string? Reason { get; set; }
}

public class AllocateLotRequest
{
    public Guid LotId { get; set; }

    public int Quantity { get; set; }

    /// <summary>Required — a manual allocation departs from the FEFO-suggested lot (FR-037).</summary>
    public string Reason { get; set; } = string.Empty;
}

public class QuarantineLotRequest
{
    public string Reason { get; set; } = string.Empty;

    public Guid QuarantineLocationId { get; set; }
}

public class RejectLotRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class DestroyLotRequest
{
    public string DestructionDocumentRef { get; set; } = string.Empty;
}
