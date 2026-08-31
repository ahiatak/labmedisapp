namespace LABMEDIS.Service.DTOs.Requests;

public class CreateShipmentRequest
{
    public string TransportMode { get; set; } = "Maritime";

    public string? Carrier { get; set; }

    public string? TransportReference { get; set; }

    public string? CustomsRegime { get; set; }

    public string? ImportAuthorizationRef { get; set; }

    public List<Guid> PurchaseOrderLineIds { get; set; } = [];
}

public class AddImportCostRequest
{
    public string CostType { get; set; } = "Freight";

    public string Amount { get; set; } = "0";

    public string AllocationKey { get; set; } = "Valeur";
}

public class AddShipmentEventRequest
{
    public string Status { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
