using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.RejectIncomingEftPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class IncomingEftPaymentRejectedConsumer(IMediator mediator)
    : IConsumer<IncomingEftPaymentRejected>
{
    public Task Consume(ConsumeContext<IncomingEftPaymentRejected> context)
    {
        return mediator.Send(
            new RejectIncomingEftPaymentCommand { Event = context.Message },
            context.CancellationToken);
    }
}
