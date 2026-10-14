using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using OpenBankingService.Application.Commands.RejectPaymentInitiation;

namespace OpenBankingService.Infrastructure.Consumers;

public sealed class OpenBankingPaymentRejectedConsumer(IMediator mediator) : IConsumer<OpenBankingPaymentRejected>
{
    public Task Consume(ConsumeContext<OpenBankingPaymentRejected> context)
    {
        var message = context.Message;
        return mediator.Send(new RejectPaymentInitiationCommand
        {
            InitiationId = message.InitiationId,
            Reason = message.Reason,
            OccurredAt = message.OccurredAt
        }, context.CancellationToken);
    }
}
