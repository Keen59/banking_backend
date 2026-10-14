using LoanService.Application.Interfaces.Repositories;
using LoanService.Domain.Entities;
using LoanService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LoanService.Infrastructure.Repositories;

public class LoanRepository(DBContext context) : Repository<Loan>(context), ILoanRepository
{
    public async Task<Loan?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.Loan
            .Include(x => x.Installments)
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<Loan?> GetByIdWithInstallmentsAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Loan
            .Include(x => x.Installments)
            .FirstOrDefaultAsync(x => x.Id == loanId, cancellationToken);
    }

    public async Task<IReadOnlyList<Loan>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Loan
            .AsNoTracking()
            .Include(x => x.Installments)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
