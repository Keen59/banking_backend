using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.CompleteIncomingFastPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class IncomingFastPaymentCompletedConsumer(IMediator mediator)
    : IConsumer<IncomingFastPaymentCompleted>
{
    public Task Consume(ConsumeContext<IncomingFastPaymentCompleted> context)
    {
        return mediator.Send(
            new CompleteIncomingFastPaymentCommand { Event = context.Message },
            context.CancellationToken);
    }
}
