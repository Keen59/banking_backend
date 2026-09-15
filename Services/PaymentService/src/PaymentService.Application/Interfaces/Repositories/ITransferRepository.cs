using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces.Repositories;

public interface ITransferRepository : IRepository<Transfer>
{
    Task<Transfer?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Transfer>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<decimal> SumCountedTowardDailyLimitAsync(
        Guid customerId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default);
}
