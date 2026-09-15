using CustomerService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace CustomerService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    ICustomerRepository CustomerRepository { get; }

    IAuditLogRepository AuditLogRepository { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
