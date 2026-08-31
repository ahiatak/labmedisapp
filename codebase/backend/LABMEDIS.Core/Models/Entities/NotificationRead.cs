namespace LABMEDIS.Core.Models.Entities;

/// <summary>Append-only per-user read marker (FR-078) — a notification is read/unread independently for each user who received it.</summary>
public class NotificationRead : AppendOnlyEntity
{
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}
