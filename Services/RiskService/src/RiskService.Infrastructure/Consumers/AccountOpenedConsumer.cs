using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using RiskService.Application.Commands.ProjectAccountOpened;

namespace RiskService.Infrastructure.Consumers;

public sealed class AccountOpenedConsumer(IMediator mediator) : IConsumer<AccountOpened>
{
    public Task Consume(ConsumeContext<AccountOpened> context)
    {
        return mediator.Send(new ProjectAccountOpenedCommand { Event = context.Message }, context.CancellationToken);
    }
}
