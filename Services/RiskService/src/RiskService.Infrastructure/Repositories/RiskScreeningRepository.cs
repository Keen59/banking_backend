using RiskService.Application.Interfaces.Repositories;
using RiskService.Domain.Entities;
using RiskService.Domain.Enums;
using RiskService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace RiskService.Infrastructure.Repositories;

public class RiskScreeningRepository(DBContext context)
    : Repository<RiskScreening>(context), IRiskScreeningRepository
{
    public async Task<RiskScreening?> GetByChannelAndSourceIdAsync(
        ERiskChannel channel,
        Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        return await Context.RiskScreening.FirstOrDefaultAsync(
            x => x.Channel == channel && x.SourceId == sourceId,
            cancellationToken);
    }

    public async Task<decimal> SumOutboundAmountForCustomerOnUtcDayAsync(
        Guid customerId,
        DateTimeOffset date,
        CancellationToken cancellationToken = default)
    {
        var from = new DateTimeOffset(date.UtcDateTime.Date, TimeSpan.Zero);
        var to = from.AddDays(1);

        return await Context.RiskScreening
            .Where(x =>
                x.CustomerId == customerId &&
                x.Direction == ERiskDirection.Outbound &&
                x.CreatedAt >= from &&
                x.CreatedAt < to)
            .SumAsync(x => x.Amount, cancellationToken);
    }

    public async Task<int> CountOutboundInBandForCustomerOnUtcDayAsync(
        Guid customerId,
        DateTimeOffset date,
        decimal minInclusive,
        decimal maxExclusive,
        CancellationToken cancellationToken = default)
    {
        var from = new DateTimeOffset(date.UtcDateTime.Date, TimeSpan.Zero);
        var to = from.AddDays(1);

        return await Context.RiskScreening
            .CountAsync(x =>
                x.CustomerId == customerId &&
                x.Direction == ERiskDirection.Outbound &&
                x.CreatedAt >= from &&
                x.CreatedAt < to &&
                x.Amount >= minInclusive &&
                x.Amount < maxExclusive,
                cancellationToken);
    }
}
