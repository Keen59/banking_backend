using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using RiskService.Application.Commands.EvaluatePayment;
using RiskService.Domain.Enums;

namespace RiskService.Infrastructure.Consumers;

public sealed class TransferRequestedConsumer(IMediator mediator) : IConsumer<TransferRequested>
{
    public Task Consume(ConsumeContext<TransferRequested> context)
    {
        var message = context.Message;
        return mediator.Send(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.Transfer,
            SourceId = message.TransferId,
            AccountId = message.SourceAccountId,
            Amount = message.Amount,
            Currency = message.Currency,
            Direction = ERiskDirection.Outbound
        }, context.CancellationToken);
    }
}
