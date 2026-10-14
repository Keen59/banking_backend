using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using LoanService.Application.Commands.RejectRepayment;

namespace LoanService.Infrastructure.Consumers;

public sealed class LoanRepaymentRejectedConsumer(IMediator mediator) : IConsumer<LoanRepaymentRejected>
{
    public Task Consume(ConsumeContext<LoanRepaymentRejected> context)
    {
        return mediator.Send(new RejectRepaymentCommand { Event = context.Message }, context.CancellationToken);
    }
}
