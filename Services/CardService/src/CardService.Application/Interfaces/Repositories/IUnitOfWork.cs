using CardService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace CardService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    ICardRepository Cards { get; }

    ICardAuthorizationRepository CardAuthorizations { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
