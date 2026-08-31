namespace LABMEDIS.Core.Models.Entities;

/// <summary>
/// Persisted real-time event (US12 — FR-076, FR-094). Always written before the SignalR push
/// is attempted, so an offline user still finds it on GET /api/notifications when they
/// reconnect. TargetRoleOrPermission names the SignalR group the event was broadcast to
/// (e.g. "Role:Direction" or "Permission:Stock.Read").
/// </summary>
public class Notification : BaseEntity
{
    public string EventType { get; set; } = string.Empty;

    public string TargetRoleOrPermission { get; set; } = string.Empty;

    /// <summary>JSON-serialized event payload (productId, lotId, etc. — see contracts/notifications.md).</summary>
    public string Payload { get; set; } = "{}";

    public bool IsCritical { get; set; }

    public DateTime? EmailSmsSentAt { get; set; }
}
