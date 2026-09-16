using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces.Repositories;

public interface IAccountProjectionRepository : IRepository<AccountProjection>
{
    Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<AccountProjection?> GetByIbanAsync(string iban, CancellationToken cancellationToken = default);
}
