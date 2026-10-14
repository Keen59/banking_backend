using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Application.DTOs.Notifications;

namespace NotificationService.Application.Commands.GetMyNotifications;

public class GetMyNotificationsCommand : IRequest<GetMyNotificationsResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyNotificationsResponse : Response
{
    public IReadOnlyList<NotificationDto> Data { get; set; } = [];
}
