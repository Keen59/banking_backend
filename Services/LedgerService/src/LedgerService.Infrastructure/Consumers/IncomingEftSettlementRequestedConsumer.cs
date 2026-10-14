using Banking.Contracts.Events;
using LedgerService.Application.Commands.SettleIncomingEftPayment;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class IncomingEftSettlementRequestedConsumer(IMediator mediator)
    : IConsumer<IncomingEftSettlementRequested>
{
    public Task Consume(ConsumeContext<IncomingEftSettlementRequested> context)
    {
        return mediator.Send(new SettleIncomingEftPaymentCommand
        {
            IncomingEftPaymentId = context.Message.IncomingEftPaymentId,
            DestinationAccountId = context.Message.DestinationAccountId,
            Description = context.Message.Description
        }, context.CancellationToken);
    }
}
