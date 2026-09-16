using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.CompleteFastPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class FastPaymentCompletedConsumer(IMediator mediator) : IConsumer<FastPaymentCompleted>
{
    public Task Consume(ConsumeContext<FastPaymentCompleted> context)
    {
        return mediator.Send(new CompleteFastPaymentCommand { Event = context.Message }, context.CancellationToken);
    }
}
