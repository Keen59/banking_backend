using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Entities;
using LedgerService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace LedgerService.Infrastructure.Repositories;

public class UnitOfWork(
    DBContext context,
    ILedgerAccountRepository accounts,
    IJournalEntryRepository journalEntries,
    IAccountHoldRepository holds) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public ILedgerAccountRepository LedgerAccounts => accounts;

    public IJournalEntryRepository JournalEntries => journalEntries;

    public IAccountHoldRepository Holds => holds;

    public async ValueTask DisposeAsync() => await context.DisposeAsync();

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new()
        => new Repository<TEntity>(context);

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        return await context.CompleteSaveAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null)
            return _currentTransaction;

        _currentTransaction = await context.Database.BeginTransactionAsync();
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await context.SaveChangesAsync();
            await _currentTransaction!.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            await _currentTransaction!.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_currentTransaction != null)
                await _currentTransaction.RollbackAsync();
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }
}
