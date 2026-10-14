using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.HoldIncomingEftPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class IncomingEftPaymentHeldConsumer(IMediator mediator) : IConsumer<IncomingEftPaymentHeld>
{
    public Task Consume(ConsumeContext<IncomingEftPaymentHeld> context)
    {
        return mediator.Send(
            new MarkIncomingEftPaymentHeldCommand { Event = context.Message },
            context.CancellationToken);
    }
}
