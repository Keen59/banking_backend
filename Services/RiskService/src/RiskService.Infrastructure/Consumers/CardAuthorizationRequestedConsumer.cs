using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using RiskService.Application.Commands.EvaluatePayment;
using RiskService.Domain.Enums;

namespace RiskService.Infrastructure.Consumers;

public sealed class CardAuthorizationRequestedConsumer(IMediator mediator)
    : IConsumer<CardAuthorizationRequested>
{
    public Task Consume(ConsumeContext<CardAuthorizationRequested> context)
    {
        var message = context.Message;
        return mediator.Send(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.Card,
            SourceId = message.AuthorizationId,
            AccountId = message.AccountId,
            Amount = message.Amount,
            Currency = message.Currency,
            Direction = ERiskDirection.Outbound
        }, context.CancellationToken);
    }
}
