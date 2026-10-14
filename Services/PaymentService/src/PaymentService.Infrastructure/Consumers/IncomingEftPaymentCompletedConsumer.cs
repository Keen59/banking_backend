using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.CompleteIncomingEftPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class IncomingEftPaymentCompletedConsumer(IMediator mediator)
    : IConsumer<IncomingEftPaymentCompleted>
{
    public Task Consume(ConsumeContext<IncomingEftPaymentCompleted> context)
    {
        return mediator.Send(
            new CompleteIncomingEftPaymentCommand { Event = context.Message },
            context.CancellationToken);
    }
}
