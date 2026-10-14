using CardService.Domain.Entities;

namespace CardService.Application.Interfaces.Repositories;

public interface ICardAuthorizationRepository : IRepository<CardAuthorization>
{
    Task<CardAuthorization?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CardAuthorization>> GetByCardIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default);
}
