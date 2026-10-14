using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using LoanService.Application.Commands.CompleteDisbursement;

namespace LoanService.Infrastructure.Consumers;

public sealed class LoanDisbursementPostedConsumer(IMediator mediator) : IConsumer<LoanDisbursementPosted>
{
    public Task Consume(ConsumeContext<LoanDisbursementPosted> context)
    {
        return mediator.Send(new CompleteDisbursementCommand { Event = context.Message }, context.CancellationToken);
    }
}
