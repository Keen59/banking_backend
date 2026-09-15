using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage;

namespace LedgerService.UnitTests;

internal sealed class InMemoryLedgerUnitOfWork : IUnitOfWork
{
    public List<LedgerAccount> Accounts { get; } = [];
    public List<JournalEntry> Entries { get; } = [];
    public List<AccountHold> HoldRows { get; } = [];

    public ILedgerAccountRepository LedgerAccounts { get; }
    public IJournalEntryRepository JournalEntries { get; }
    public IAccountHoldRepository Holds { get; }

    public InMemoryLedgerUnitOfWork()
    {
        LedgerAccounts = new MemoryAccounts(this);
        JournalEntries = new MemoryJournal(this);
        Holds = new MemoryHolds(this);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new()
        => throw new NotSupportedException();

    public Task<int> SaveAsync(CancellationToken cancellationToken) => Task.FromResult(1);

    public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotSupportedException();

    public Task CommitTransactionAsync() => throw new NotSupportedException();

    public Task RollbackTransactionAsync() => throw new NotSupportedException();

    private sealed class MemoryAccounts(InMemoryLedgerUnitOfWork store) : ILedgerAccountRepository
    {
        public IQueryable<LedgerAccount> Query() => store.Accounts.AsQueryable();

        public Task<LedgerAccount?> GetByIdAsync(Guid id)
            => Task.FromResult(store.Accounts.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(LedgerAccount entity, CancellationToken cancellationToken)
        {
            store.Accounts.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(LedgerAccount entity) { }

        public void Remove(LedgerAccount entity) => store.Accounts.Remove(entity);

        public Task<LedgerAccount?> GetBySourceAccountIdAsync(Guid sourceAccountId, CancellationToken cancellationToken = default)
            => Task.FromResult(store.Accounts.FirstOrDefault(x => x.SourceAccountId == sourceAccountId));

        public Task<IReadOnlyList<LedgerAccount>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LedgerAccount>>(
                store.Accounts.Where(x => x.CustomerId == customerId).ToList());

        public Task<bool> ExistsBySourceAccountIdAsync(Guid sourceAccountId, CancellationToken cancellationToken = default)
            => Task.FromResult(store.Accounts.Any(x => x.SourceAccountId == sourceAccountId));

        public Task<IReadOnlyList<LedgerAccount>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LedgerAccount>>(
                store.Accounts.Where(x => ids.Contains(x.Id)).ToList());
    }

    private sealed class MemoryJournal(InMemoryLedgerUnitOfWork store) : IJournalEntryRepository
    {
        public IQueryable<JournalEntry> Query() => store.Entries.AsQueryable();

        public Task<JournalEntry?> GetByIdAsync(Guid id)
            => Task.FromResult(store.Entries.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(JournalEntry entity, CancellationToken cancellationToken)
        {
            foreach (var line in entity.Lines)
                line.JournalEntry = entity;
            store.Entries.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(JournalEntry entity) { }

        public void Remove(JournalEntry entity) => store.Entries.Remove(entity);

        public Task<JournalEntry?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
            => Task.FromResult(store.Entries.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<JournalLine>> GetLinesByLedgerAccountIdAsync(
            Guid ledgerAccountId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<JournalLine>>(
                store.Entries.SelectMany(entry => entry.Lines)
                    .Where(line => line.LedgerAccountId == ledgerAccountId)
                    .ToList());
    }

    private sealed class MemoryHolds(InMemoryLedgerUnitOfWork store) : IAccountHoldRepository
    {
        public IQueryable<AccountHold> Query() => store.HoldRows.AsQueryable();

        public Task<AccountHold?> GetByIdAsync(Guid id)
            => Task.FromResult(store.HoldRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(AccountHold entity, CancellationToken cancellationToken)
        {
            store.HoldRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(AccountHold entity) { }

        public void Remove(AccountHold entity) => store.HoldRows.Remove(entity);

        public Task<AccountHold?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
            => Task.FromResult(store.HoldRows.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<AccountHold>> GetActiveByLedgerAccountIdAsync(
            Guid ledgerAccountId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AccountHold>>(
                store.HoldRows
                    .Where(x => x.LedgerAccountId == ledgerAccountId && x.Status == EHoldStatus.Active)
                    .ToList());
    }
}
