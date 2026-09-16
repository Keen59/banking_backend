using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.RejectTestCredit;

namespace PaymentService.Infrastructure.Consumers;

public sealed class TestCreditRejectedConsumer(IMediator mediator) : IConsumer<TestCreditRejected>
{
    public Task Consume(ConsumeContext<TestCreditRejected> context)
    {
        return mediator.Send(new RejectTestCreditCommand { Event = context.Message }, context.CancellationToken);
    }
}
