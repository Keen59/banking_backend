using Banking.Contracts.Events;
using LedgerService.Application.Commands.ReturnEftPayment;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class EftReturnRequestedConsumer(IMediator mediator) : IConsumer<EftReturnRequested>
{
    public Task Consume(ConsumeContext<EftReturnRequested> context)
    {
        return mediator.Send(new ReturnEftPaymentCommand
        {
            EftPaymentId = context.Message.EftPaymentId,
            Reason = context.Message.Reason
        }, context.CancellationToken);
    }
}
