using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using OpenBankingService.Application.Commands.RejectPaymentInitiation;

namespace OpenBankingService.Infrastructure.Consumers;

public sealed class TransferRejectedConsumer(IMediator mediator) : IConsumer<TransferRejected>
{
    public Task Consume(ConsumeContext<TransferRejected> context)
    {
        var message = context.Message;
        return mediator.Send(new RejectPaymentInitiationCommand
        {
            InitiationId = message.TransferId,
            Reason = message.Reason,
            OccurredAt = message.OccurredAt
        }, context.CancellationToken);
    }
}
