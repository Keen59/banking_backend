using Banking.Contracts.Events;
using CardService.Application.Commands.IssueCardFromAccountOpened;
using MassTransit;
using MediatR;

namespace CardService.Infrastructure.Consumers;

public sealed class AccountOpenedConsumer(IMediator mediator) : IConsumer<AccountOpened>
{
    public Task Consume(ConsumeContext<AccountOpened> context)
    {
        return mediator.Send(new IssueCardFromAccountOpenedCommand { Event = context.Message }, context.CancellationToken);
    }
}
