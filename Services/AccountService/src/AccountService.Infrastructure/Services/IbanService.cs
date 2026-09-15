using System.Security.Cryptography;
using AccountService.Application.Helpers;
using AccountService.Application.Interfaces.Repositories;
using AccountService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace AccountService.Infrastructure.Services;

public sealed class IbanService(IAccountRepository accountRepository, IConfiguration configuration) : IIbanService
{
    public async Task<(string Iban, string AccountNumber)> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var bankCode = configuration["Bank:Code"] ?? "00100";
        if (bankCode.Length != 5 || !bankCode.All(char.IsDigit))
            throw new InvalidOperationException("Bank:Code 5 haneli rakam olmalıdır.");

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var accountNumber = RandomNumberGenerator.GetInt32(0, 1_000_000_000).ToString("D9")
                                + RandomNumberGenerator.GetInt32(0, 10_000_000).ToString("D7");
            var iban = TurkishIbanCalculator.Build(bankCode, accountNumber);
            if (!await accountRepository.IbanExistsAsync(iban, cancellationToken))
                return (iban, accountNumber);
        }

        throw new InvalidOperationException("IBAN üretilemedi.");
    }
}
