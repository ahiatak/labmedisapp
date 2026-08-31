using LABMEDIS.Core.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = LABMEDIS.Core.Models.Entities.Notification;
using NotificationReadEntity = LABMEDIS.Core.Models.Entities.NotificationRead;

namespace LABMEDIS.Core.Repositories.Notification;

public class NotificationRepository(AppDbContext context) : BaseRepository<NotificationEntity>(context), INotificationRepository
{
    public async Task<IReadOnlyList<NotificationEntity>> GetForGroupsAsync(IReadOnlyCollection<string> groups, bool unreadOnly, Guid userId, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(n => groups.Contains(n.TargetRoleOrPermission) || n.TargetRoleOrPermission == "All");

        if (unreadOnly)
        {
            var readIds = await Context.Set<NotificationReadEntity>().Where(r => r.UserId == userId).Select(r => r.NotificationId).ToListAsync(cancellationToken);
            query = query.Where(n => !readIds.Contains(n.Id));
        }

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<bool> IsReadByUserAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        Context.Set<NotificationReadEntity>().AnyAsync(r => r.NotificationId == notificationId && r.UserId == userId, cancellationToken);

    public async Task MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (await IsReadByUserAsync(notificationId, userId, cancellationToken))
        {
            return;
        }

        await Context.Set<NotificationReadEntity>().AddAsync(new NotificationReadEntity { NotificationId = notificationId, UserId = userId }, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(IReadOnlyCollection<string> groups, Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await GetForGroupsAsync(groups, unreadOnly: true, userId, cancellationToken);
        foreach (var notification in unread)
        {
            await Context.Set<NotificationReadEntity>().AddAsync(new NotificationReadEntity { NotificationId = notification.Id, UserId = userId }, cancellationToken);
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}
