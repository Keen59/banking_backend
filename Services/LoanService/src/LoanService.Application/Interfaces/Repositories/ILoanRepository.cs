using LoanService.Domain.Entities;

namespace LoanService.Application.Interfaces.Repositories;

public interface ILoanRepository : IRepository<Loan>
{
    Task<Loan?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Loan?> GetByIdWithInstallmentsAsync(
        Guid loanId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Loan>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
