using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces.Repositories;

public interface ICustomerNotificationRepository : IRepository<CustomerNotification>
{
    Task<IReadOnlyList<CustomerNotification>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerNotification?> GetByTypeAndSourceIdAsync(
        string type,
        Guid sourceId,
        CancellationToken cancellationToken = default);
}
