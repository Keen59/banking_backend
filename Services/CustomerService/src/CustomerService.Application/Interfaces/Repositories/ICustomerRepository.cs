using CustomerService.Domain.Entities;

namespace CustomerService.Application.Interfaces.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByIdWithDetailsAsync(Guid id, bool asNoTracking = false, CancellationToken cancellationToken = default);

    Task<Customer?> GetByCifNumberAsync(string cifNumber, bool asNoTracking = false, CancellationToken cancellationToken = default);

    Task<Customer?> GetByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default);

    Task<bool> CifExistsAsync(string cifNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> ListPendingKycAsync(CancellationToken cancellationToken = default);
}
