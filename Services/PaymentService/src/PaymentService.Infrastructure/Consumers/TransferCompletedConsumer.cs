using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.CompleteTransfer;

namespace PaymentService.Infrastructure.Consumers;

public sealed class TransferCompletedConsumer(IMediator mediator) : IConsumer<TransferCompleted>
{
    public Task Consume(ConsumeContext<TransferCompleted> context)
    {
        return mediator.Send(new CompleteTransferCommand { Event = context.Message }, context.CancellationToken);
    }
}
