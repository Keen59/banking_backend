using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.RejectEftPayment;

namespace PaymentService.Infrastructure.Consumers;

public sealed class EftPaymentRejectedConsumer(IMediator mediator) : IConsumer<EftPaymentRejected>
{
    public Task Consume(ConsumeContext<EftPaymentRejected> context)
    {
        return mediator.Send(new RejectEftPaymentCommand { Event = context.Message }, context.CancellationToken);
    }
}
