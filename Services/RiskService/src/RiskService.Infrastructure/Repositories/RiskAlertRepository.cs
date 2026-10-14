using RiskService.Application.Interfaces.Repositories;
using RiskService.Domain.Entities;
using RiskService.Domain.Enums;
using RiskService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace RiskService.Infrastructure.Repositories;

public class RiskAlertRepository(DBContext context)
    : Repository<RiskAlert>(context), IRiskAlertRepository
{
    public async Task<IReadOnlyList<RiskAlert>> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        return await Context.RiskAlert
            .AsNoTracking()
            .Where(x => x.Status == EAlertStatus.Open)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
