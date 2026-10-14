using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces.Repositories;

public interface IFxConversionRepository : IRepository<FxConversion>
{
    Task<FxConversion?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FxConversion>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<decimal> SumCountedTowardDailyLimitAsync(
        Guid customerId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default);
}
