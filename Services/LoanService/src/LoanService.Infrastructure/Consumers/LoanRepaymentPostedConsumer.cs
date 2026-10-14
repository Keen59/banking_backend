using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using LoanService.Application.Commands.CompleteRepayment;

namespace LoanService.Infrastructure.Consumers;

public sealed class LoanRepaymentPostedConsumer(IMediator mediator) : IConsumer<LoanRepaymentPosted>
{
    public Task Consume(ConsumeContext<LoanRepaymentPosted> context)
    {
        return mediator.Send(new CompleteRepaymentCommand { Event = context.Message }, context.CancellationToken);
    }
}
