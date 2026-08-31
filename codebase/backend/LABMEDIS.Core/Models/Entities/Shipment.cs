namespace LABMEDIS.Core.Models.Entities;

/// <summary>Shipment tracking states — driven by POST /api/shipments/{id}/events (contracts/shipments.md).</summary>
public enum ShipmentStatus
{
    Creee = 0,
    Expediee = 1,
    ArriveePort = 2,
    EnDouane = 3,
    Dedouanee = 4,
    Livree = 5
}

/// <summary>Logistics shipment (US3 — FR-025 à FR-028), can group one or more purchase orders via ShipmentLine.</summary>
public class Shipment : BaseEntity
{
    public string ShipmentNumber { get; set; } = string.Empty;

    public TransportMode TransportMode { get; set; }

    public string? Carrier { get; set; }

    public string? TransportReference { get; set; }

    public string? CustomsRegime { get; set; }

    public DateOnly? DepartureDateEstimated { get; set; }

    public DateOnly? DepartureDateActual { get; set; }

    public DateOnly? ArrivalDateEstimated { get; set; }

    public DateOnly? ArrivalDateActual { get; set; }

    public ShipmentStatus Status { get; set; } = ShipmentStatus.Creee;

    /// <summary>DPML import authorization reference — mandatory when the shipment carries a medicine (FR-028).</summary>
    public string? ImportAuthorizationRef { get; set; }

    public List<ShipmentLine> Lines { get; set; } = [];

    public List<ImportCost> Costs { get; set; } = [];

    public List<ShipmentEvent> Events { get; set; } = [];
}
