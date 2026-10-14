using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using RiskService.Application.Commands.EvaluatePayment;
using RiskService.Domain.Enums;

namespace RiskService.Infrastructure.Consumers;

public sealed class EftPaymentRequestedConsumer(IMediator mediator) : IConsumer<EftPaymentRequested>
{
    public Task Consume(ConsumeContext<EftPaymentRequested> context)
    {
        var message = context.Message;
        return mediator.Send(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.Eft,
            SourceId = message.EftPaymentId,
            AccountId = message.SourceAccountId,
            Amount = message.Amount,
            Currency = message.Currency,
            Direction = ERiskDirection.Outbound
        }, context.CancellationToken);
    }
}
