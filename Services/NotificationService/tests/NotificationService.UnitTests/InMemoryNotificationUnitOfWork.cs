using NotificationService.Application.Interfaces.Repositories;
using NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace NotificationService.UnitTests;

internal sealed class InMemoryNotificationUnitOfWork : IUnitOfWork
{
    public List<CustomerNotification> NotificationRows { get; } = [];

    public ICustomerNotificationRepository CustomerNotifications { get; }

    public InMemoryNotificationUnitOfWork()
    {
        CustomerNotifications = new MemoryCustomerNotifications(this);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new()
        => throw new NotSupportedException();

    public Task<int> SaveAsync(CancellationToken cancellationToken) => Task.FromResult(1);

    public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotSupportedException();

    public Task CommitTransactionAsync() => throw new NotSupportedException();

    public Task RollbackTransactionAsync() => throw new NotSupportedException();

    private sealed class MemoryCustomerNotifications(InMemoryNotificationUnitOfWork store)
        : ICustomerNotificationRepository
    {
        public IQueryable<CustomerNotification> Query() => store.NotificationRows.AsQueryable();

        public Task<CustomerNotification?> GetByIdAsync(Guid id)
            => Task.FromResult(store.NotificationRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(CustomerNotification entity, CancellationToken cancellationToken)
        {
            store.NotificationRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(CustomerNotification entity) { }

        public void Remove(CustomerNotification entity) => store.NotificationRows.Remove(entity);

        public Task<IReadOnlyList<CustomerNotification>> GetByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CustomerNotification>>(
                store.NotificationRows
                    .Where(x => x.CustomerId == customerId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList());

        public Task<CustomerNotification?> GetByTypeAndSourceIdAsync(
            string type,
            Guid sourceId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.NotificationRows.FirstOrDefault(
                x => x.Type == type && x.SourceId == sourceId));
    }
}
