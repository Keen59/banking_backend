using MediatR;
using NotificationService.Application.Interfaces.Repositories;
using NotificationService.Application.Mapping;

namespace NotificationService.Application.Commands.GetMyNotifications;

public class GetMyNotificationsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyNotificationsCommand, GetMyNotificationsResponse>
{
    public async Task<GetMyNotificationsResponse> Handle(
        GetMyNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        var notifications = await unitOfWork.CustomerNotifications.GetByCustomerIdAsync(
            request.CustomerId,
            cancellationToken);

        return new GetMyNotificationsResponse
        {
            Message = "Notifications listed.",
            Data = notifications.Select(NotificationMapper.ToDto).ToList()
        };
    }
}
