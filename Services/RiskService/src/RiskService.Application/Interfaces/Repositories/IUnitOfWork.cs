using RiskService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace RiskService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IAccountProjectionRepository AccountProjections { get; }

    IRiskScreeningRepository RiskScreenings { get; }

    IRiskAlertRepository RiskAlerts { get; }

    ICustomerProfileRepository CustomerProfiles { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
