using OpenBankingService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace OpenBankingService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IAccountProjectionRepository AccountProjections { get; }

    IConsentRepository Consents { get; }

    IPaymentInitiationRepository PaymentInitiations { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
