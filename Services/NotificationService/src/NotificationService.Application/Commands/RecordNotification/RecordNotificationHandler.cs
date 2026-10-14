using MediatR;
using NotificationService.Application.Interfaces.Repositories;
using NotificationService.Application.Mapping;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Commands.RecordNotification;

public class RecordNotificationHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RecordNotificationCommand, RecordNotificationResponse>
{
    public async Task<RecordNotificationResponse> Handle(
        RecordNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.CustomerNotifications.GetByTypeAndSourceIdAsync(
            request.Type,
            request.SourceId,
            cancellationToken);

        if (existing is not null)
        {
            return new RecordNotificationResponse
            {
                Message = "Existing notification returned for type and source.",
                Data = NotificationMapper.ToDto(existing)
            };
        }

        var notification = new CustomerNotification
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Type = request.Type,
            Title = request.Title,
            Body = request.Body,
            SourceId = request.SourceId,
            IsRead = false,
            CreatedAt = request.OccurredAt == default ? default : request.OccurredAt
        };

        await unitOfWork.CustomerNotifications.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new RecordNotificationResponse
        {
            Message = "Notification recorded.",
            Data = NotificationMapper.ToDto(notification)
        };
    }
}
