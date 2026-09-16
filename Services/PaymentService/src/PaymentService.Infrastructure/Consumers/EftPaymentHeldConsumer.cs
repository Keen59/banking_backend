using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.HoldEftPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class EftPaymentHeldConsumer(IMediator mediator) : IConsumer<EftPaymentHeld>
{
    public Task Consume(ConsumeContext<EftPaymentHeld> context)
    {
        return mediator.Send(new MarkEftPaymentHeldCommand { Event = context.Message }, context.CancellationToken);
    }
}
