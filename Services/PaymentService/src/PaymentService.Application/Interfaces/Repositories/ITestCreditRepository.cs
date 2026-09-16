using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces.Repositories;

public interface ITestCreditRepository : IRepository<TestCredit>
{
    Task<TestCredit?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
