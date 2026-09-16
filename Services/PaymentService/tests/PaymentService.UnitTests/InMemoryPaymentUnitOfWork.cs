using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage;

namespace PaymentService.UnitTests;

internal sealed class InMemoryPaymentUnitOfWork : IUnitOfWork
{
    public List<AccountProjection> Accounts { get; } = [];
    public List<Transfer> TransferRows { get; } = [];
    public List<FastPayment> FastPaymentRows { get; } = [];
    public List<EftPayment> EftPaymentRows { get; } = [];
    public List<TestCredit> TestCreditRows { get; } = [];
    public List<IncomingFastPayment> IncomingFastPaymentRows { get; } = [];

    public IAccountProjectionRepository AccountProjections { get; }
    public ITransferRepository Transfers { get; }
    public IFastPaymentRepository FastPayments { get; }
    public IEftPaymentRepository EftPayments { get; }
    public ITestCreditRepository TestCredits { get; }
    public IIncomingFastPaymentRepository IncomingFastPayments { get; }

    public InMemoryPaymentUnitOfWork()
    {
        AccountProjections = new MemoryAccounts(this);
        Transfers = new MemoryTransfers(this);
        FastPayments = new MemoryFastPayments(this);
        EftPayments = new MemoryEftPayments(this);
        TestCredits = new MemoryTestCredits(this);
        IncomingFastPayments = new MemoryIncomingFastPayments(this);
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

        public Task<AccountProjection?> GetByIbanAsync(string iban, CancellationToken cancellationToken = default)
            => Task.FromResult(store.Accounts.FirstOrDefault(x => x.Iban == iban));
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

    private sealed class MemoryFastPayments(InMemoryPaymentUnitOfWork store) : IFastPaymentRepository
    {
        public IQueryable<FastPayment> Query() => store.FastPaymentRows.AsQueryable();

        public Task<FastPayment?> GetByIdAsync(Guid id)
            => Task.FromResult(store.FastPaymentRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(FastPayment entity, CancellationToken cancellationToken)
        {
            store.FastPaymentRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(FastPayment entity) { }

        public void Remove(FastPayment entity) => store.FastPaymentRows.Remove(entity);

        public Task<FastPayment?> GetByIdempotencyKeyAsync(
            Guid customerId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.FastPaymentRows.FirstOrDefault(
                x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<FastPayment>> GetByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FastPayment>>(
                store.FastPaymentRows.Where(x => x.CustomerId == customerId).ToList());

        public Task<decimal> SumCountedTowardDailyLimitAsync(
            Guid customerId,
            DateTimeOffset fromUtc,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                store.FastPaymentRows
                    .Where(x =>
                        x.CustomerId == customerId &&
                        x.CreatedAt >= fromUtc &&
                        x.Status != ETransferStatus.Rejected)
                    .Sum(x => x.Amount));
    }

    private sealed class MemoryEftPayments(InMemoryPaymentUnitOfWork store) : IEftPaymentRepository
    {
        public IQueryable<EftPayment> Query() => store.EftPaymentRows.AsQueryable();

        public Task<EftPayment?> GetByIdAsync(Guid id)
            => Task.FromResult(store.EftPaymentRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(EftPayment entity, CancellationToken cancellationToken)
        {
            store.EftPaymentRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(EftPayment entity) { }

        public void Remove(EftPayment entity) => store.EftPaymentRows.Remove(entity);

        public Task<EftPayment?> GetByIdempotencyKeyAsync(
            Guid customerId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.EftPaymentRows.FirstOrDefault(
                x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<EftPayment>> GetByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EftPayment>>(
                store.EftPaymentRows.Where(x => x.CustomerId == customerId).ToList());

        public Task<decimal> SumCountedTowardDailyLimitAsync(
            Guid customerId,
            DateTimeOffset fromUtc,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                store.EftPaymentRows
                    .Where(x =>
                        x.CustomerId == customerId &&
                        x.CreatedAt >= fromUtc &&
                        x.Status != ETransferStatus.Rejected)
                    .Sum(x => x.Amount));
    }

    private sealed class MemoryTestCredits(InMemoryPaymentUnitOfWork store) : ITestCreditRepository
    {
        public IQueryable<TestCredit> Query() => store.TestCreditRows.AsQueryable();

        public Task<TestCredit?> GetByIdAsync(Guid id)
            => Task.FromResult(store.TestCreditRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(TestCredit entity, CancellationToken cancellationToken)
        {
            store.TestCreditRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(TestCredit entity) { }

        public void Remove(TestCredit entity) => store.TestCreditRows.Remove(entity);

        public Task<TestCredit?> GetByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.TestCreditRows.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey));
    }

    private sealed class MemoryIncomingFastPayments(InMemoryPaymentUnitOfWork store) : IIncomingFastPaymentRepository
    {
        public IQueryable<IncomingFastPayment> Query() => store.IncomingFastPaymentRows.AsQueryable();

        public Task<IncomingFastPayment?> GetByIdAsync(Guid id)
            => Task.FromResult(store.IncomingFastPaymentRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(IncomingFastPayment entity, CancellationToken cancellationToken)
        {
            store.IncomingFastPaymentRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(IncomingFastPayment entity) { }

        public void Remove(IncomingFastPayment entity) => store.IncomingFastPaymentRows.Remove(entity);

        public Task<IncomingFastPayment?> GetByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.IncomingFastPaymentRows.FirstOrDefault(x => x.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<IncomingFastPayment>> GetByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IncomingFastPayment>>(
                store.IncomingFastPaymentRows.Where(x => x.AccountCustomerId == customerId).ToList());
    }
}
