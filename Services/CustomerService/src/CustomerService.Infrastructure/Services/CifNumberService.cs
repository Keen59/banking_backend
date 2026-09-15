using System.Security.Cryptography;
using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Interfaces.Services;

namespace CustomerService.Infrastructure.Services;

public class CifNumberService(ICustomerRepository customerRepository) : ICifNumberService
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var cif = RandomNumberGenerator.GetInt32(1_000_000_000, 2_000_000_000).ToString();
            if (!await customerRepository.CifExistsAsync(cif, cancellationToken))
                return cif;
        }

        throw new InvalidOperationException("CIF numarası üretilemedi.");
    }
}
