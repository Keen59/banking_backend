using Banking.Contracts.Events;
using LedgerService.Application.Commands.ReturnIncomingEftPayment;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class IncomingEftReturnRequestedConsumer(IMediator mediator)
    : IConsumer<IncomingEftReturnRequested>
{
    public Task Consume(ConsumeContext<IncomingEftReturnRequested> context)
    {
        return mediator.Send(new ReturnIncomingEftPaymentCommand
        {
            IncomingEftPaymentId = context.Message.IncomingEftPaymentId,
            Reason = context.Message.Reason
        }, context.CancellationToken);
    }
}
