using LoanService.Application.Interfaces.Repositories;
using LoanService.Domain.Entities;
using LoanService.Domain.Enums;
using LoanService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LoanService.Infrastructure.Repositories;

public class LoanInstallmentRepository(DBContext context)
    : Repository<LoanInstallment>(context), ILoanInstallmentRepository
{
    public async Task<IReadOnlyList<LoanInstallment>> GetByLoanIdAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await Context.LoanInstallment
            .Where(x => x.LoanId == loanId)
            .OrderBy(x => x.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task<LoanInstallment?> GetNextPendingAsync(
        Guid loanId,
        CancellationToken cancellationToken = default)
    {
        return await Context.LoanInstallment
            .Where(x => x.LoanId == loanId && x.Status == EInstallmentStatus.Pending)
            .OrderBy(x => x.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
