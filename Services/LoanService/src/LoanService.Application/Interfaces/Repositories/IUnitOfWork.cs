using LoanService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoanService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IAsyncDisposable
{
    IAccountProjectionRepository AccountProjections { get; }

    ILoanRepository Loans { get; }

    ILoanInstallmentRepository Installments { get; }

    IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new();

    Task<int> SaveAsync(CancellationToken cancellationToken);

    Task<IDbContextTransaction> BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
