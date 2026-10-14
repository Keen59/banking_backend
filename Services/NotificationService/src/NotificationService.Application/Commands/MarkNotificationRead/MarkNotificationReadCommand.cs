using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Application.DTOs.Notifications;

namespace NotificationService.Application.Commands.MarkNotificationRead;

public class MarkNotificationReadCommand : IRequest<MarkNotificationReadResponse>
{
    public Guid NotificationId { get; set; }

    public Guid CustomerId { get; set; }
}

public class MarkNotificationReadResponse : Response
{
    public NotificationDto? Data { get; set; }
}
