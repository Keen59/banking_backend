using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage;

namespace PaymentService.UnitTests;

internal sealed class InMemoryPaymentUnitOfWork : IUnitOfWork
{
    public List<AccountProjection> Accounts { get; } = [];
    public List<Transfer> TransferRows { get; } = [];

    public IAccountProjectionRepository AccountProjections { get; }
    public ITransferRepository Transfers { get; }

    public InMemoryPaymentUnitOfWork()
    {
        AccountProjections = new MemoryAccounts(this);
        Transfers = new MemoryTransfers(this);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new()
        => throw new NotSupportedException();

    public Task<int> SaveAsync(CancellationToken cancellationToken) => Task.FromResult(1);

    public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotSupportedException();

    public Task CommitTransactionAsync() => throw new NotSupportedException();

    public Task RollbackTransactionAsync() => throw new NotSupportedException();

    private sealed class MemoryAccounts(InMemoryPaymentUnitOfWork store) : IAccountProjectionRepository
    {
        public IQueryable<AccountProjection> Query() => store.Accounts.AsQueryable();

        public Task<AccountProjection?> GetByIdAsync(Guid id)
            => Task.FromResult(store.Accounts.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(AccountProjection entity, CancellationToken cancellationToken)
        {
            store.Accounts.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(AccountProjection entity) { }

        public void Remove(AccountProjection entity) => store.Accounts.Remove(entity);

        public Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default)
            => Task.FromResult(store.Accounts.Any(x => x.Id == accountId));
    }

    private sealed class MemoryTransfers(InMemoryPaymentUnitOfWork store) : ITransferRepository
    {
        public IQueryable<Transfer> Query() => store.TransferRows.AsQueryable();

        public Task<Transfer?> GetByIdAsync(Guid id)
            => Task.FromResult(store.TransferRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(Transfer entity, CancellationToken cancellationToken)
        {
            store.TransferRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Transfer entity) { }

        public void Remove(Transfer entity) => store.TransferRows.Remove(entity);

        public Task<Transfer?> GetByIdempotencyKeyAsync(
            Guid customerId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.TransferRows.FirstOrDefault(
                x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<Transfer>> GetByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Transfer>>(
                store.TransferRows.Where(x => x.CustomerId == customerId).ToList());

        public Task<decimal> SumCountedTowardDailyLimitAsync(
            Guid customerId,
            DateTimeOffset fromUtc,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                store.TransferRows
                    .Where(x =>
                        x.CustomerId == customerId &&
                        x.CreatedAt >= fromUtc &&
                        x.Status != ETransferStatus.Rejected)
                    .Sum(x => x.Amount));
    }
}
