using LABMEDIS.Service.Extensions;
using ImportCostEntity = LABMEDIS.Core.Models.Entities.ImportCost;
using ShipmentEntity = LABMEDIS.Core.Models.Entities.Shipment;
using ShipmentEventEntity = LABMEDIS.Core.Models.Entities.ShipmentEvent;

namespace LABMEDIS.Service.DTOs.Responses;

public class ShipmentResponse
{
    public Guid Id { get; set; }

    public string ShipmentNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string TransportMode { get; set; } = string.Empty;

    public string? Carrier { get; set; }

    public string? TransportReference { get; set; }

    public string? CustomsRegime { get; set; }

    public string? ImportAuthorizationRef { get; set; }

    public ShipmentResponse()
    {
    }

    public ShipmentResponse(ShipmentEntity entity)
    {
        Id = entity.Id;
        ShipmentNumber = entity.ShipmentNumber;
        Status = entity.Status.ToString();
        TransportMode = entity.TransportMode.ToString();
        Carrier = entity.Carrier;
        TransportReference = entity.TransportReference;
        CustomsRegime = entity.CustomsRegime;
        ImportAuthorizationRef = entity.ImportAuthorizationRef;
    }
}

public class ImportCostResponse
{
    public Guid Id { get; set; }

    public string CostType { get; set; } = string.Empty;

    public string Amount { get; set; } = "0";

    public string AllocationKey { get; set; } = string.Empty;

    public ImportCostResponse(ImportCostEntity entity)
    {
        Id = entity.Id;
        CostType = entity.CostType.ToString();
        Amount = entity.Amount.ToInvariantString("0.##");
        AllocationKey = entity.AllocationKey.ToString();
    }
}

public class ShipmentTimelineEntryResponse
{
    public string Status { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public string? Notes { get; set; }

    public ShipmentTimelineEntryResponse(ShipmentEventEntity entity)
    {
        Status = entity.Status.ToString();
        OccurredAt = entity.OccurredAt;
        Notes = entity.Notes;
    }
}
