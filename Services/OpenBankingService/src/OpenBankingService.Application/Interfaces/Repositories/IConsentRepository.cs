using OpenBankingService.Domain.Entities;

namespace OpenBankingService.Application.Interfaces.Repositories;

public interface IConsentRepository : IRepository<Consent>
{
    Task<Consent?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<Consent?> GetByAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<Consent?> GetByIdWithAccountsAsync(
        Guid consentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Consent>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
