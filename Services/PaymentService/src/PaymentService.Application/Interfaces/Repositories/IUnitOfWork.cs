using PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace PaymentService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IAccountProjectionRepository AccountProjections { get; }

    ITransferRepository Transfers { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
