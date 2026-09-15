using AccountService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace AccountService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IAccountRepository AccountRepository { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
