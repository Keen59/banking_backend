using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.CompleteEftPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class EftPaymentCompletedConsumer(IMediator mediator) : IConsumer<EftPaymentCompleted>
{
    public Task Consume(ConsumeContext<EftPaymentCompleted> context)
    {
        return mediator.Send(new CompleteEftPaymentCommand { Event = context.Message }, context.CancellationToken);
    }
}
