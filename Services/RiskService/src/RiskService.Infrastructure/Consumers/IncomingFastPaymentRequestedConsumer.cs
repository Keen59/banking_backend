using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using RiskService.Application.Commands.EvaluatePayment;
using RiskService.Domain.Enums;

namespace RiskService.Infrastructure.Consumers;

public sealed class IncomingFastPaymentRequestedConsumer(IMediator mediator)
    : IConsumer<IncomingFastPaymentRequested>
{
    public Task Consume(ConsumeContext<IncomingFastPaymentRequested> context)
    {
        var message = context.Message;
        return mediator.Send(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.IncomingFast,
            SourceId = message.IncomingFastPaymentId,
            AccountId = message.DestinationAccountId,
            Amount = message.Amount,
            Currency = message.Currency,
            Direction = ERiskDirection.Inbound
        }, context.CancellationToken);
    }
}
