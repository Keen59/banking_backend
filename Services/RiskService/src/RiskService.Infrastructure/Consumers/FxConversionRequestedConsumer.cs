using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using RiskService.Application.Commands.EvaluatePayment;
using RiskService.Domain.Enums;

namespace RiskService.Infrastructure.Consumers;

public sealed class FxConversionRequestedConsumer(IMediator mediator) : IConsumer<FxConversionRequested>
{
    public Task Consume(ConsumeContext<FxConversionRequested> context)
    {
        var message = context.Message;
        return mediator.Send(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.Fx,
            SourceId = message.ConversionId,
            AccountId = message.SourceAccountId,
            Amount = message.TryEquivalent,
            Currency = "TRY",
            Direction = ERiskDirection.Outbound
        }, context.CancellationToken);
    }
}
