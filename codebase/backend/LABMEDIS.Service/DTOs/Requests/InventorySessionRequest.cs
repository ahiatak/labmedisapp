namespace LABMEDIS.Service.DTOs.Requests;

public class CreateInventorySessionRequest
{
    public string Perimeter { get; set; } = string.Empty;
}

public class RecordCountRequest
{
    public Guid StockLotId { get; set; }

    public int CountedQuantity { get; set; }
}

public class ValidateInventorySessionRequest
{
    /// <summary>Required if any count shows a non-zero variance (FR-044).</summary>
    public string? AdjustmentReason { get; set; }
}
