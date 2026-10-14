using NotificationService.Application.DTOs.Notifications;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Mapping;

public static class NotificationMapper
{
    public static NotificationDto ToDto(CustomerNotification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            CustomerId = notification.CustomerId,
            Type = notification.Type,
            Title = notification.Title,
            Body = notification.Body,
            SourceId = notification.SourceId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
