using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using RiskService.Application.Commands.EvaluatePayment;
using RiskService.Domain.Enums;

namespace RiskService.Infrastructure.Consumers;

public sealed class IncomingEftPaymentRequestedConsumer(IMediator mediator)
    : IConsumer<IncomingEftPaymentRequested>
{
    public Task Consume(ConsumeContext<IncomingEftPaymentRequested> context)
    {
        var message = context.Message;
        return mediator.Send(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.IncomingEft,
            SourceId = message.IncomingEftPaymentId,
            AccountId = message.DestinationAccountId,
            Amount = message.Amount,
            Currency = message.Currency,
            Direction = ERiskDirection.Inbound
        }, context.CancellationToken);
    }
}
