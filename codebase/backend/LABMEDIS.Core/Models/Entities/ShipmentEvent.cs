namespace LABMEDIS.Core.Models.Entities;

/// <summary>Append-only tracking event (contracts/shipments.md — GET .../timeline).</summary>
public class ShipmentEvent : AppendOnlyEntity
{
    public Guid ShipmentId { get; set; }

    public ShipmentStatus Status { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}
