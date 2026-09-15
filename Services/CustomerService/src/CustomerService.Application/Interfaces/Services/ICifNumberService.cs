namespace CustomerService.Application.Interfaces.Services;

public interface ICifNumberService
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
