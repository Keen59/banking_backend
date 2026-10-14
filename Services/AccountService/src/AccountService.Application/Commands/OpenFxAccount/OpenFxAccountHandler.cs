using AccountService.Application.Interfaces.Repositories;
using AccountService.Application.Interfaces.Services;
using AccountService.Application.Mapping;
using AccountService.Domain.Entities;
using AccountService.Domain.Enums;
using Banking.Contracts.Events;
using MediatR;

namespace AccountService.Application.Commands.OpenFxAccount;

public class OpenFxAccountHandler(
    IUnitOfWork unitOfWork,
    IIbanService ibanService,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<OpenFxAccountCommand, OpenFxAccountResponse>
{
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase) { "USD", "EUR" };

    public async Task<OpenFxAccountResponse> Handle(
        OpenFxAccountCommand request,
        CancellationToken cancellationToken)
    {
        var currency = request.Currency.Trim().ToUpperInvariant();
        if (!Supported.Contains(currency))
            throw new InvalidOperationException("Desteklenen döviz: USD, EUR. TRY hesabı KYC onayı ile açılır.");

        var existing = await unitOfWork.AccountRepository.GetByCustomerIdAsync(
            request.CustomerId,
            cancellationToken);

        var tryAccount = existing.FirstOrDefault(x =>
            x.ProductType == EAccountProductType.DemandDeposit &&
            x.Status == EAccountStatus.Active &&
            string.Equals(x.Currency, "TRY", StringComparison.OrdinalIgnoreCase));

        if (tryAccount is null)
            throw new InvalidOperationException("Döviz hesabı yalnızca KYC sonrası vadesiz TRY hesabı olan müşteriye açılır.");

        var already = existing.FirstOrDefault(x =>
            x.ProductType == EAccountProductType.DemandDeposit &&
            string.Equals(x.Currency, currency, StringComparison.OrdinalIgnoreCase));

        if (already is not null)
        {
            return new OpenFxAccountResponse
            {
                Message = "Döviz hesabı zaten açık.",
                Account = AccountMapper.ToDto(already)
            };
        }

        var (iban, accountNumber) = await ibanService.GenerateAsync(cancellationToken);
        var account = new Account
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CifNumber = tryAccount.CifNumber,
            Iban = iban,
            AccountNumber = accountNumber,
            ProductType = EAccountProductType.DemandDeposit,
            Status = EAccountStatus.Active,
            Currency = currency
        };

        await unitOfWork.AccountRepository.AddAsync(account, cancellationToken);
        await eventPublisher.PublishAsync(
            new AccountOpened(
                account.Id,
                account.CustomerId,
                account.CifNumber,
                account.Iban,
                account.Currency,
                DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new OpenFxAccountResponse
        {
            Message = "Döviz hesabı açıldı.",
            Account = AccountMapper.ToDto(account)
        };
    }
}
