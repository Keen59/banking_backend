using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using OpenBankingService.Application.Commands.CompletePaymentInitiation;

namespace OpenBankingService.Infrastructure.Consumers;

public sealed class TransferCompletedConsumer(IMediator mediator) : IConsumer<TransferCompleted>
{
    public Task Consume(ConsumeContext<TransferCompleted> context)
    {
        return mediator.Send(new CompletePaymentInitiationCommand { Event = context.Message }, context.CancellationToken);
    }
}
