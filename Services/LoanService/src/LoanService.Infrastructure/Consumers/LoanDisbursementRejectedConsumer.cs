using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using LoanService.Application.Commands.RejectDisbursement;

namespace LoanService.Infrastructure.Consumers;

public sealed class LoanDisbursementRejectedConsumer(IMediator mediator) : IConsumer<LoanDisbursementRejected>
{
    public Task Consume(ConsumeContext<LoanDisbursementRejected> context)
    {
        return mediator.Send(new RejectDisbursementCommand { Event = context.Message }, context.CancellationToken);
    }
}
