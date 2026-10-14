using LoanService.Application.Interfaces.Repositories;
using LoanService.Domain.Entities;
using LoanService.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage;

namespace LoanService.UnitTests;

internal sealed class InMemoryLoanUnitOfWork : IUnitOfWork
{
    public List<AccountProjection> Accounts { get; } = [];
    public List<Loan> LoanRows { get; } = [];
    public List<LoanInstallment> InstallmentRows { get; } = [];

    public IAccountProjectionRepository AccountProjections { get; }
    public ILoanRepository Loans { get; }
    public ILoanInstallmentRepository Installments { get; }

    public InMemoryLoanUnitOfWork()
    {
        AccountProjections = new MemoryAccounts(this);
        Loans = new MemoryLoans(this);
        Installments = new MemoryInstallments(this);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public IRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity, new()
        => throw new NotSupportedException();

    public Task<int> SaveAsync(CancellationToken cancellationToken) => Task.FromResult(1);

    public Task<IDbContextTransaction> BeginTransactionAsync() => throw new NotSupportedException();

    public Task CommitTransactionAsync() => throw new NotSupportedException();

    public Task RollbackTransactionAsync() => throw new NotSupportedException();

    private void AttachInstallments(Loan loan)
    {
        loan.Installments = InstallmentRows.Where(x => x.LoanId == loan.Id).OrderBy(x => x.Sequence).ToList();
    }

    private sealed class MemoryAccounts(InMemoryLoanUnitOfWork store) : IAccountProjectionRepository
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

    private sealed class MemoryLoans(InMemoryLoanUnitOfWork store) : ILoanRepository
    {
        public IQueryable<Loan> Query() => store.LoanRows.AsQueryable();

        public Task<Loan?> GetByIdAsync(Guid id)
        {
            var loan = store.LoanRows.FirstOrDefault(x => x.Id == id);
            if (loan is not null)
                store.AttachInstallments(loan);
            return Task.FromResult(loan);
        }

        public Task AddAsync(Loan entity, CancellationToken cancellationToken)
        {
            store.LoanRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Loan entity) { }

        public void Remove(Loan entity) => store.LoanRows.Remove(entity);

        public Task<Loan?> GetByIdempotencyKeyAsync(
            Guid customerId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            var loan = store.LoanRows.FirstOrDefault(
                x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey);
            if (loan is not null)
                store.AttachInstallments(loan);
            return Task.FromResult(loan);
        }

        public Task<Loan?> GetByIdWithInstallmentsAsync(
            Guid loanId,
            CancellationToken cancellationToken = default)
            => GetByIdAsync(loanId);

        public Task<IReadOnlyList<Loan>> GetByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
        {
            var loans = store.LoanRows.Where(x => x.CustomerId == customerId).ToList();
            foreach (var loan in loans)
                store.AttachInstallments(loan);
            return Task.FromResult<IReadOnlyList<Loan>>(loans);
        }
    }

    private sealed class MemoryInstallments(InMemoryLoanUnitOfWork store) : ILoanInstallmentRepository
    {
        public IQueryable<LoanInstallment> Query() => store.InstallmentRows.AsQueryable();

        public Task<LoanInstallment?> GetByIdAsync(Guid id)
            => Task.FromResult(store.InstallmentRows.FirstOrDefault(x => x.Id == id));

        public Task AddAsync(LoanInstallment entity, CancellationToken cancellationToken)
        {
            if (store.InstallmentRows.All(x => x.Id != entity.Id))
                store.InstallmentRows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(LoanInstallment entity) { }

        public void Remove(LoanInstallment entity) => store.InstallmentRows.Remove(entity);

        public Task<IReadOnlyList<LoanInstallment>> GetByLoanIdAsync(
            Guid loanId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<LoanInstallment>>(
                store.InstallmentRows.Where(x => x.LoanId == loanId).OrderBy(x => x.Sequence).ToList());

        public Task<LoanInstallment?> GetNextPendingAsync(
            Guid loanId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                store.InstallmentRows
                    .Where(x => x.LoanId == loanId && x.Status == EInstallmentStatus.Pending)
                    .OrderBy(x => x.Sequence)
                    .FirstOrDefault());
    }
}
