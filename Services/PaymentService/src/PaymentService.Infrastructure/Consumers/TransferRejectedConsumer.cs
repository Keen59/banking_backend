using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.RejectTransfer;

namespace PaymentService.Infrastructure.Consumers;

public sealed class TransferRejectedConsumer(IMediator mediator) : IConsumer<TransferRejected>
{
    public Task Consume(ConsumeContext<TransferRejected> context)
    {
        return mediator.Send(new RejectTransferCommand { Event = context.Message }, context.CancellationToken);
    }
}
