using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.RejectIncomingFastPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class IncomingFastPaymentRejectedConsumer(IMediator mediator)
    : IConsumer<IncomingFastPaymentRejected>
{
    public Task Consume(ConsumeContext<IncomingFastPaymentRejected> context)
    {
        return mediator.Send(
            new RejectIncomingFastPaymentCommand { Event = context.Message },
            context.CancellationToken);
    }
}
