using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace OpenBankingService.UnitTests;

internal sealed class InMemoryOpenBankingUnitOfWork : IUnitOfWork
{
    public List<AccountProjection> Accounts { get; } = [];
    public List<Consent> ConsentRows { get; } = [];
    public List<PaymentInitiation> InitiationRows { get; } = [];

    public IAccountProjectionRepository AccountProjections { get; }
    public IConsentRepository Consents { get; }
    public IPaymentInitiationRepository PaymentInitiations { get; }

    public InMemoryOpenBankingUnitOfWork()
    {
        AccountProjections = new MemoryAccounts(this);
        Consents = new MemoryConsents(this);
        PaymentInitiations = new MemoryInitiations(this);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new()
        => throw new NotSupportedException();

    public Task<int> SaveAsync(CancellationToken cancellationToken) => Task.FromResult(1);

    public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotSupportedException();

    public Task CommitTransactionAsync() => throw new NotSupportedException();

    public Task RollbackTransactionAsync() => throw new NotSupportedException();

    private sealed class MemoryAccounts(InMemoryOpenBankingUnitOfWork store) : IAccountProjectionRepository
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

    private sealed class MemoryConsents(InMemoryOpenBankingUnitOfWork store) : IConsentRepository
    {
        public IQueryable<Consent> Query() => store.ConsentRows.AsQueryable();

        public Task<Consent?> GetByIdAsync(Guid id)
            => Task.FromResult(store.ConsentRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(Consent entity, CancellationToken cancellationToken)
        {
            store.ConsentRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Consent entity) { }

        public void Remove(Consent entity) => store.ConsentRows.Remove(entity);

        public Task<Consent?> GetByIdempotencyKeyAsync(
            Guid customerId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.ConsentRows.FirstOrDefault(
                x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey));

        public Task<Consent?> GetByAccessTokenAsync(
            string accessToken,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.ConsentRows.FirstOrDefault(x => x.AccessToken == accessToken));

        public Task<Consent?> GetByIdWithAccountsAsync(
            Guid consentId,
            CancellationToken cancellationToken = default)
            => GetByIdAsync(consentId);

        public Task<IReadOnlyList<Consent>> GetByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Consent>>(
                store.ConsentRows.Where(x => x.CustomerId == customerId).ToList());
    }

    private sealed class MemoryInitiations(InMemoryOpenBankingUnitOfWork store) : IPaymentInitiationRepository
    {
        public IQueryable<PaymentInitiation> Query() => store.InitiationRows.AsQueryable();

        public Task<PaymentInitiation?> GetByIdAsync(Guid id)
            => Task.FromResult(store.InitiationRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(PaymentInitiation entity, CancellationToken cancellationToken)
        {
            store.InitiationRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(PaymentInitiation entity) { }

        public void Remove(PaymentInitiation entity) => store.InitiationRows.Remove(entity);

        public Task<PaymentInitiation?> GetByIdempotencyKeyAsync(
            Guid customerId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.InitiationRows.FirstOrDefault(
                x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey));
    }
}
