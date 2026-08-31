using LABMEDIS.Core.Repositories.Base;
using NotificationEntity = LABMEDIS.Core.Models.Entities.Notification;

namespace LABMEDIS.Core.Repositories.Notification;

public interface INotificationRepository : IBaseRepository<NotificationEntity>
{
    /// <summary>Notifications addressed to any of the caller's groups ("Role:X"/"Permission:Y") or the special "All" broadcast group, newest first.</summary>
    Task<IReadOnlyList<NotificationEntity>> GetForGroupsAsync(IReadOnlyCollection<string> groups, bool unreadOnly, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsReadByUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(IReadOnlyCollection<string> groups, Guid userId, CancellationToken cancellationToken = default);
}
