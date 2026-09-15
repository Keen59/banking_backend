using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Domain.Entities;
using CustomerService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace CustomerService.Infrastructure.Repositories;

public class UnitOfWork(
    DBContext context,
    ICustomerRepository customers,
    IAuditLogRepository auditLogs) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public ICustomerRepository CustomerRepository => customers;

    public IAuditLogRepository AuditLogRepository => auditLogs;

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
