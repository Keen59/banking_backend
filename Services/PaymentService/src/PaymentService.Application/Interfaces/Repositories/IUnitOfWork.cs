using PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace PaymentService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IAccountProjectionRepository AccountProjections { get; }

    ITransferRepository Transfers { get; }

    IFastPaymentRepository FastPayments { get; }

    IEftPaymentRepository EftPayments { get; }

    ITestCreditRepository TestCredits { get; }

    IIncomingFastPaymentRepository IncomingFastPayments { get; }

    IIncomingEftPaymentRepository IncomingEftPayments { get; }

    IFxConversionRepository FxConversions { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
