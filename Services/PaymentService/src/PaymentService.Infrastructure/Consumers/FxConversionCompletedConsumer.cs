using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.CompleteFxConversion;

namespace PaymentService.Infrastructure.Consumers;

public sealed class FxConversionCompletedConsumer(IMediator mediator) : IConsumer<FxConversionCompleted>
{
    public Task Consume(ConsumeContext<FxConversionCompleted> context)
        => mediator.Send(new CompleteFxConversionCommand { Event = context.Message }, context.CancellationToken);
}
