using System.Text.Json;
using LABMEDIS.Core;
using LABMEDIS.Core.Repositories.Notification;
using LABMEDIS.Service.DTOs.Responses;
using LABMEDIS.Service.Hubs;
using LABMEDIS.Service.Logging;
using Microsoft.AspNetCore.SignalR;
using NotificationEntity = LABMEDIS.Core.Models.Entities.Notification;

namespace LABMEDIS.Service.Services;

/// <summary>
/// Real-time notifications (US12 — FR-076 à FR-079, FR-094). Inherits NotificationRepository
/// directly (Principle II); IHubContext&lt;NotificationHub&gt; is injected (composition) to
/// push events from outside the Hub itself.
/// </summary>
public class NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext, ILoggerManager logger)
    : NotificationRepository(context), INotificationService
{
    public async Task EmitAsync(string eventType, string targetGroup, object payload, bool isCritical = false, CancellationToken cancellationToken = default)
    {
        var notification = new NotificationEntity
        {
            EventType = eventType,
            TargetRoleOrPermission = targetGroup,
            Payload = JsonSerializer.Serialize(payload),
            IsCritical = isCritical
        };

        // Persisted BEFORE the SignalR push (FR-094) — an offline recipient still finds it
        // via GET /api/notifications on reconnection even if the push below never reaches them.
        await AddAsync(notification, cancellationToken);

        await hubContext.Clients.Group(targetGroup).SendAsync(eventType, payload, cancellationToken);
        await hubContext.Clients.Group(targetGroup).SendAsync("notification:new", new { notification.Id, eventType }, cancellationToken);

        if (isCritical)
        {
            // Fire-and-forget secondary channel (FR-079) — never blocks the SignalR push above.
            // Actual FluentEmail/Twilio dispatch requires SMTP/Twilio credentials to be
            // configured (none are provisioned in this environment); until then this only
            // logs the attempt so the integration point is visible and easy to complete.
            _ = Task.Run(async () =>
            {
                try
                {
                    logger.LogInfo($"NotificationService | Alerte critique '{eventType}' à relayer par email/SMS (non configuré) — notification {notification.Id}.");
                    notification.EmailSmsSentAt = DateTime.UtcNow;
                    await UpdateAsync(notification, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"NotificationService | Échec du relais email/SMS pour la notification {notification.Id}.");
                }
            }, CancellationToken.None);
        }
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(
        Guid userId, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, bool unreadOnly, CancellationToken cancellationToken = default)
    {
        var groups = BuildGroups(roles, permissions);
        var notifications = await GetForGroupsAsync(groups, unreadOnly, userId, cancellationToken);

        var result = new List<NotificationResponse>();
        foreach (var notification in notifications)
        {
            var isRead = await IsReadByUserAsync(notification.Id, userId, cancellationToken);
            result.Add(new NotificationResponse(notification, isRead));
        }

        return result;
    }

    Task INotificationService.MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken) =>
        MarkReadAsync(notificationId, userId, cancellationToken);

    public Task MarkAllReadAsync(Guid userId, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default) =>
        MarkAllReadAsync(BuildGroups(roles, permissions), userId, cancellationToken);

    private static List<string> BuildGroups(IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions) =>
        roles.Select(r => $"Role:{r}").Concat(permissions.Select(p => $"Permission:{p}")).ToList();
}
