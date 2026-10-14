using NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace NotificationService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    ICustomerNotificationRepository CustomerNotifications { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
