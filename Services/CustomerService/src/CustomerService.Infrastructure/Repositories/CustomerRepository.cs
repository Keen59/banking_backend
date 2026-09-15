using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Enums;
using CustomerService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Repositories;

public class CustomerRepository(DBContext context) : Repository<Customer>(context), ICustomerRepository
{
    public async Task<Customer?> GetByIdWithDetailsAsync(
        Guid id,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Customer> query = DetailsQuery();

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetByCifNumberAsync(
        string cifNumber,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Customer> query = DetailsQuery();

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(x => x.CifNumber == cifNumber, cancellationToken);
    }

    public async Task<Customer?> GetByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        return await Context.Customer.FirstOrDefaultAsync(x => x.NationalId == nationalId, cancellationToken);
    }

    public async Task<bool> CifExistsAsync(string cifNumber, CancellationToken cancellationToken = default)
    {
        return await Context.Customer.AnyAsync(x => x.CifNumber == cifNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListPendingKycAsync(CancellationToken cancellationToken = default)
    {
        return await DetailsQuery()
            .AsNoTracking()
            .Where(x => x.KycStatus == EKycStatus.Pending ||
                        x.KycStatus == EKycStatus.InReview)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Customer> DetailsQuery()
    {
        return Context.Customer
            .Include(x => x.Addresses)
            .Include(x => x.Documents)
            .Include(x => x.Consents);
    }
}
