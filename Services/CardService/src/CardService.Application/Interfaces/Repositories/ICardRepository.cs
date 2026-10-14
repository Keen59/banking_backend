using CardService.Domain.Entities;

namespace CardService.Application.Interfaces.Repositories;

public interface ICardRepository : IRepository<Card>
{
    Task<IReadOnlyList<Card>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);
}
