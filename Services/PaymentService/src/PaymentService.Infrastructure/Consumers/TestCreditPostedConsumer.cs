using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.CompleteTestCredit;

namespace PaymentService.Infrastructure.Consumers;

public sealed class TestCreditPostedConsumer(IMediator mediator) : IConsumer<TestCreditPosted>
{
    public Task Consume(ConsumeContext<TestCreditPosted> context)
    {
        return mediator.Send(new CompleteTestCreditCommand { Event = context.Message }, context.CancellationToken);
    }
}
