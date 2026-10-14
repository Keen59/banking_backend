using NotificationService.Application.Interfaces.Repositories;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Repositories;

public class CustomerNotificationRepository(DBContext context)
    : Repository<CustomerNotification>(context), ICustomerNotificationRepository
{
    public async Task<IReadOnlyList<CustomerNotification>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.CustomerNotification
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerNotification?> GetByTypeAndSourceIdAsync(
        string type,
        Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        return await Context.CustomerNotification.FirstOrDefaultAsync(
            x => x.Type == type && x.SourceId == sourceId,
            cancellationToken);
    }
}
