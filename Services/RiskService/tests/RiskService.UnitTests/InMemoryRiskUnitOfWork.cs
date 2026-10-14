using RiskService.Application.Interfaces.Repositories;
using RiskService.Domain.Entities;
using RiskService.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage;

namespace RiskService.UnitTests;

internal sealed class InMemoryRiskUnitOfWork : IUnitOfWork
{
    public List<AccountProjection> Accounts { get; } = [];
    public List<RiskScreening> ScreeningRows { get; } = [];
    public List<RiskAlert> AlertRows { get; } = [];
    public List<CustomerProfile> ProfileRows { get; } = [];

    public IAccountProjectionRepository AccountProjections { get; }
    public IRiskScreeningRepository RiskScreenings { get; }
    public IRiskAlertRepository RiskAlerts { get; }
    public ICustomerProfileRepository CustomerProfiles { get; }

    public InMemoryRiskUnitOfWork()
    {
        AccountProjections = new MemoryAccounts(this);
        RiskScreenings = new MemoryScreenings(this);
        RiskAlerts = new MemoryAlerts(this);
        CustomerProfiles = new MemoryProfiles(this);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new()
        => throw new NotSupportedException();

    public Task<int> SaveAsync(CancellationToken cancellationToken) => Task.FromResult(1);

    public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotSupportedException();

    public Task CommitTransactionAsync() => throw new NotSupportedException();

    public Task RollbackTransactionAsync() => throw new NotSupportedException();

    private sealed class MemoryAccounts(InMemoryRiskUnitOfWork store) : IAccountProjectionRepository
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

    private sealed class MemoryScreenings(InMemoryRiskUnitOfWork store) : IRiskScreeningRepository
    {
        public IQueryable<RiskScreening> Query() => store.ScreeningRows.AsQueryable();

        public Task<RiskScreening?> GetByIdAsync(Guid id)
            => Task.FromResult(store.ScreeningRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(RiskScreening entity, CancellationToken cancellationToken)
        {
            store.ScreeningRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(RiskScreening entity) { }

        public void Remove(RiskScreening entity) => store.ScreeningRows.Remove(entity);

        public Task<RiskScreening?> GetByChannelAndSourceIdAsync(
            ERiskChannel channel,
            Guid sourceId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(store.ScreeningRows.FirstOrDefault(
                x => x.Channel == channel && x.SourceId == sourceId));

        public Task<decimal> SumOutboundAmountForCustomerOnUtcDayAsync(
            Guid customerId,
            DateTimeOffset date,
            CancellationToken cancellationToken = default)
        {
            var from = new DateTimeOffset(date.UtcDateTime.Date, TimeSpan.Zero);
            var to = from.AddDays(1);
            return Task.FromResult(
                store.ScreeningRows
                    .Where(x =>
                        x.CustomerId == customerId &&
                        x.Direction == ERiskDirection.Outbound &&
                        x.CreatedAt >= from &&
                        x.CreatedAt < to)
                    .Sum(x => x.Amount));
        }

        public Task<int> CountOutboundInBandForCustomerOnUtcDayAsync(
            Guid customerId,
            DateTimeOffset date,
            decimal minInclusive,
            decimal maxExclusive,
            CancellationToken cancellationToken = default)
        {
            var from = new DateTimeOffset(date.UtcDateTime.Date, TimeSpan.Zero);
            var to = from.AddDays(1);
            return Task.FromResult(
                store.ScreeningRows.Count(x =>
                    x.CustomerId == customerId &&
                    x.Direction == ERiskDirection.Outbound &&
                    x.CreatedAt >= from &&
                    x.CreatedAt < to &&
                    x.Amount >= minInclusive &&
                    x.Amount < maxExclusive));
        }
    }

    private sealed class MemoryAlerts(InMemoryRiskUnitOfWork store) : IRiskAlertRepository
    {
        public IQueryable<RiskAlert> Query() => store.AlertRows.AsQueryable();

        public Task<RiskAlert?> GetByIdAsync(Guid id)
            => Task.FromResult(store.AlertRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(RiskAlert entity, CancellationToken cancellationToken)
        {
            store.AlertRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(RiskAlert entity) { }

        public void Remove(RiskAlert entity) => store.AlertRows.Remove(entity);

        public Task<IReadOnlyList<RiskAlert>> GetOpenAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RiskAlert>>(
                store.AlertRows
                    .Where(x => x.Status == EAlertStatus.Open)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList());
    }

    private sealed class MemoryProfiles(InMemoryRiskUnitOfWork store) : ICustomerProfileRepository
    {
        public IQueryable<CustomerProfile> Query() => store.ProfileRows.AsQueryable();

        public Task<CustomerProfile?> GetByIdAsync(Guid id)
            => Task.FromResult(store.ProfileRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(CustomerProfile entity, CancellationToken cancellationToken)
        {
            store.ProfileRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(CustomerProfile entity) { }

        public void Remove(CustomerProfile entity) => store.ProfileRows.Remove(entity);

        public Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(store.ProfileRows.Any(x => x.Id == customerId));
    }
}
