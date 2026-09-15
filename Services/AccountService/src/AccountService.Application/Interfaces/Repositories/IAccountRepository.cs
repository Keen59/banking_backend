using AccountService.Domain.Entities;
using AccountService.Domain.Enums;

namespace AccountService.Application.Interfaces.Repositories;

public interface IAccountRepository : IRepository<Account>
{
    Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid customerId,
        EAccountProductType productType,
        string currency,
        CancellationToken cancellationToken = default);

    Task<bool> IbanExistsAsync(string iban, CancellationToken cancellationToken = default);
}
