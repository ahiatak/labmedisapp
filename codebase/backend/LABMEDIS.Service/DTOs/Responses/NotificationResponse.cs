using NotificationEntity = LABMEDIS.Core.Models.Entities.Notification;

namespace LABMEDIS.Service.DTOs.Responses;

public class NotificationResponse
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = "{}";

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public NotificationResponse()
    {
    }

    public NotificationResponse(NotificationEntity entity, bool isRead)
    {
        Id = entity.Id;
        EventType = entity.EventType;
        Payload = entity.Payload;
        IsRead = isRead;
        CreatedAt = entity.CreatedAt;
    }
}
