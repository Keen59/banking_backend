using CardService.Application.Interfaces.Repositories;
using CardService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace CardService.UnitTests;

internal sealed class InMemoryCardUnitOfWork : IUnitOfWork
{
    public List<Card> CardRows { get; } = [];
    public List<CardAuthorization> AuthorizationRows { get; } = [];

    public ICardRepository Cards { get; }
    public ICardAuthorizationRepository CardAuthorizations { get; }

    public InMemoryCardUnitOfWork()
    {
        Cards = new MemoryCards(this);
        CardAuthorizations = new MemoryAuthorizations(this);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new()
        => throw new NotSupportedException();

    public Task<int> SaveAsync(CancellationToken cancellationToken) => Task.FromResult(1);

    public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotSupportedException();

    public Task CommitTransactionAsync() => throw new NotSupportedException();

    public Task RollbackTransactionAsync() => throw new NotSupportedException();

    private sealed class MemoryCards(InMemoryCardUnitOfWork store) : ICardRepository
    {
        public IQueryable<Card> Query() => store.CardRows.AsQueryable();

        public Task<Card?> GetByIdAsync(Guid id)
            => Task.FromResult(store.CardRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(Card entity, CancellationToken cancellationToken)
        {
            store.CardRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Card entity) { }

        public void Remove(Card entity) => store.CardRows.Remove(entity);

        public Task<IReadOnlyList<Card>> GetByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Card>>(
                store.CardRows.Where(x => x.CustomerId == customerId).ToList());

        public Task<bool> ExistsByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.CardRows.Any(x => x.AccountId == accountId));
    }

    private sealed class MemoryAuthorizations(InMemoryCardUnitOfWork store) : ICardAuthorizationRepository
    {
        public IQueryable<CardAuthorization> Query() => store.AuthorizationRows.AsQueryable();

        public Task<CardAuthorization?> GetByIdAsync(Guid id)
            => Task.FromResult(store.AuthorizationRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(CardAuthorization entity, CancellationToken cancellationToken)
        {
            store.AuthorizationRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(CardAuthorization entity) { }

        public void Remove(CardAuthorization entity) => store.AuthorizationRows.Remove(entity);

        public Task<CardAuthorization?> GetByIdempotencyKeyAsync(
            Guid customerId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.AuthorizationRows.FirstOrDefault(
                x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey));

        public Task<IReadOnlyList<CardAuthorization>> GetByCardIdAsync(
            Guid cardId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CardAuthorization>>(
                store.AuthorizationRows.Where(x => x.CardId == cardId).ToList());
    }
}
