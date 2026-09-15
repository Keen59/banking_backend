using Banking.Contracts.Events;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MassTransit;

namespace LedgerService.Infrastructure.Consumers;

public sealed class AccountOpenedConsumer(IUnitOfWork unitOfWork) : IConsumer<AccountOpened>
{
    public async Task Consume(ConsumeContext<AccountOpened> context)
    {
        var message = context.Message;
        var exists = await unitOfWork.LedgerAccounts.ExistsBySourceAccountIdAsync(
            message.AccountId,
            context.CancellationToken);

        if (exists)
            return;

        await unitOfWork.LedgerAccounts.AddAsync(new LedgerAccount
        {
            Id = Guid.NewGuid(),
            SourceAccountId = message.AccountId,
            CustomerId = message.CustomerId,
            Currency = message.Currency,
            Kind = ELedgerAccountKind.CustomerDemandDeposit,
            Status = ELedgerAccountStatus.Active
        }, context.CancellationToken);

        await unitOfWork.SaveAsync(context.CancellationToken);
    }
}
