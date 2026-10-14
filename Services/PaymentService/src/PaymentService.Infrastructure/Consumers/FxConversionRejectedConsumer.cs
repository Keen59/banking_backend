using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.RejectFxConversion;

namespace PaymentService.Infrastructure.Consumers;

public sealed class FxConversionRejectedConsumer(IMediator mediator) : IConsumer<FxConversionRejected>
{
    public Task Consume(ConsumeContext<FxConversionRejected> context)
        => mediator.Send(new RejectFxConversionCommand { Event = context.Message }, context.CancellationToken);
}
