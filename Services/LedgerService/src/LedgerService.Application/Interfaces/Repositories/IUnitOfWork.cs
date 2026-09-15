using LedgerService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace LedgerService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    ILedgerAccountRepository LedgerAccounts { get; }

    IJournalEntryRepository JournalEntries { get; }

    IAccountHoldRepository Holds { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
