using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Repositories;

public class TestCreditRepository(DBContext context) : Repository<TestCredit>(context), ITestCreditRepository
{
    public async Task<TestCredit?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.TestCredit.FirstOrDefaultAsync(
            x => x.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }
}
