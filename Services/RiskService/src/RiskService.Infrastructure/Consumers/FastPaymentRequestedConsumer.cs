using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using RiskService.Application.Commands.EvaluatePayment;
using RiskService.Domain.Enums;

namespace RiskService.Infrastructure.Consumers;

public sealed class FastPaymentRequestedConsumer(IMediator mediator) : IConsumer<FastPaymentRequested>
{
    public Task Consume(ConsumeContext<FastPaymentRequested> context)
    {
        var message = context.Message;
        return mediator.Send(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.Fast,
            SourceId = message.FastPaymentId,
            AccountId = message.SourceAccountId,
            Amount = message.Amount,
            Currency = message.Currency,
            Direction = ERiskDirection.Outbound
        }, context.CancellationToken);
    }
}
