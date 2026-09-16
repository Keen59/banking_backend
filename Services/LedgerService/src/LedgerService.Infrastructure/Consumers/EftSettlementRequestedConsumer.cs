using Banking.Contracts.Events;
using LedgerService.Application.Commands.SettleEftPayment;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class EftSettlementRequestedConsumer(IMediator mediator) : IConsumer<EftSettlementRequested>
{
    public Task Consume(ConsumeContext<EftSettlementRequested> context)
    {
        return mediator.Send(new SettleEftPaymentCommand
        {
            EftPaymentId = context.Message.EftPaymentId
        }, context.CancellationToken);
    }
}
