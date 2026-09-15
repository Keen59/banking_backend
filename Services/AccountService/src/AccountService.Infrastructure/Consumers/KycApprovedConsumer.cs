using AccountService.Application.Interfaces.Repositories;
using AccountService.Application.Interfaces.Services;
using AccountService.Domain.Entities;
using AccountService.Domain.Enums;
using Banking.Contracts.Events;
using MassTransit;

namespace AccountService.Infrastructure.Consumers;

public sealed class KycApprovedConsumer(
    IUnitOfWork unitOfWork,
    IIbanService ibanService) : IConsumer<KycApproved>
{
    private const string Currency = "TRY";

    public async Task Consume(ConsumeContext<KycApproved> context)
    {
        var message = context.Message;
        var exists = await unitOfWork.AccountRepository.ExistsAsync(
            message.CustomerId,
            EAccountProductType.DemandDeposit,
            Currency,
            context.CancellationToken);

        if (exists)
            return;

        var (iban, accountNumber) = await ibanService.GenerateAsync(context.CancellationToken);

        await unitOfWork.AccountRepository.AddAsync(new Account
        {
            Id = Guid.NewGuid(),
            CustomerId = message.CustomerId,
            CifNumber = message.CifNumber,
            Iban = iban,
            AccountNumber = accountNumber,
            ProductType = EAccountProductType.DemandDeposit,
            Status = EAccountStatus.Active,
            Currency = Currency
        }, context.CancellationToken);

        await unitOfWork.SaveAsync(context.CancellationToken);
    }
}
