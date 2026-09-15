namespace AccountService.Application.Interfaces.Services;

public interface IIbanService
{
    Task<(string Iban, string AccountNumber)> GenerateAsync(CancellationToken cancellationToken = default);
}
