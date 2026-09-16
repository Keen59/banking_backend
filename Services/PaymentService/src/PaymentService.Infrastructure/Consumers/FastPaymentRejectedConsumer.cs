using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.RejectFastPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class FastPaymentRejectedConsumer(IMediator mediator) : IConsumer<FastPaymentRejected>
{
    public Task Consume(ConsumeContext<FastPaymentRejected> context)
    {
        return mediator.Send(new RejectFastPaymentCommand { Event = context.Message }, context.CancellationToken);
    }
}
