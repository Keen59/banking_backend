using LoanService.Domain.Entities;

namespace LoanService.Application.Interfaces.Repositories;

public interface ILoanInstallmentRepository : IRepository<LoanInstallment>
{
    Task<IReadOnlyList<LoanInstallment>> GetByLoanIdAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<LoanInstallment?> GetNextPendingAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);
}
