using LABMEDIS.Service.DTOs.Responses;

namespace LABMEDIS.Service.Services;

public interface INotificationService
{
    /// <summary>
    /// Persists the event (FR-094 — guarantees it is found later even if no one was
    /// connected), pushes it in real time to every connection in the "Role:X"/"Permission:Y"
    /// group named by <paramref name="targetGroup"/> (use "All" to broadcast), and — when
    /// <paramref name="isCritical"/> — relays it by email/SMS asynchronously without
    /// blocking the SignalR push (FR-079).
    /// </summary>
    Task EmitAsync(string eventType, string targetGroup, object payload, bool isCritical = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(Guid userId, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, bool unreadOnly, CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(Guid userId, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default);
}
