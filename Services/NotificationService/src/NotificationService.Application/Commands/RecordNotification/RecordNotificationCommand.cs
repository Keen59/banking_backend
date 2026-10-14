using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Application.DTOs.Notifications;

namespace NotificationService.Application.Commands.RecordNotification;

public class RecordNotificationCommand : IRequest<RecordNotificationResponse>
{
    public Guid CustomerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public Guid SourceId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}

public class RecordNotificationResponse : Response
{
    public NotificationDto? Data { get; set; }
}
