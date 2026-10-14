using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using OpenBankingService.Application.Commands.ProjectAccountOpened;

namespace OpenBankingService.Infrastructure.Consumers;

public sealed class AccountOpenedConsumer(IMediator mediator) : IConsumer<AccountOpened>
{
    public Task Consume(ConsumeContext<AccountOpened> context)
    {
        return mediator.Send(new ProjectAccountOpenedCommand { Event = context.Message }, context.CancellationToken);
    }
}
