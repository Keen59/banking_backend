using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using RiskService.Application.Commands.ProjectKycApproved;

namespace RiskService.Infrastructure.Consumers;

public sealed class KycApprovedConsumer(IMediator mediator) : IConsumer<KycApproved>
{
    public Task Consume(ConsumeContext<KycApproved> context)
        => mediator.Send(new ProjectKycApprovedCommand { Event = context.Message }, context.CancellationToken);
}
