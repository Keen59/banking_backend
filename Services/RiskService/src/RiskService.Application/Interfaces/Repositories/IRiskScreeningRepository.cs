using RiskService.Domain.Entities;
using RiskService.Domain.Enums;

namespace RiskService.Application.Interfaces.Repositories;

public interface IRiskScreeningRepository : IRepository<RiskScreening>
{
    Task<RiskScreening?> GetByChannelAndSourceIdAsync(
        ERiskChannel channel,
        Guid sourceId,
        CancellationToken cancellationToken = default);

    Task<decimal> SumOutboundAmountForCustomerOnUtcDayAsync(
        Guid customerId,
        DateTimeOffset date,
        CancellationToken cancellationToken = default);

    Task<int> CountOutboundInBandForCustomerOnUtcDayAsync(
        Guid customerId,
        DateTimeOffset date,
        decimal minInclusive,
        decimal maxExclusive,
        CancellationToken cancellationToken = default);
}
