using MediatR;
using NotificationService.Application.Interfaces.Repositories;
using NotificationService.Application.Mapping;

namespace NotificationService.Application.Commands.MarkNotificationRead;

public class MarkNotificationReadHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<MarkNotificationReadCommand, MarkNotificationReadResponse>
{
    public async Task<MarkNotificationReadResponse> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await unitOfWork.CustomerNotifications.GetByIdAsync(request.NotificationId)
            ?? throw new KeyNotFoundException("Notification not found.");

        if (notification.CustomerId != request.CustomerId)
            throw new UnauthorizedAccessException("Notification does not belong to the customer.");

        notification.IsRead = true;
        unitOfWork.CustomerNotifications.Update(notification);
        await unitOfWork.SaveAsync(cancellationToken);

        return new MarkNotificationReadResponse
        {
            Message = "Notification marked as read.",
            Data = NotificationMapper.ToDto(notification)
        };
    }
}
