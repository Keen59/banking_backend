using Banking.Contracts.Events;
using LedgerService.Application.Commands.ApplyFxConversion;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class FxConversionRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<FxConversionRequested>
{
    public async Task Consume(ConsumeContext<FxConversionRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new ApplyFxConversionCommand
            {
                ConversionId = message.ConversionId,
                SourceAccountId = message.SourceAccountId,
                DestinationAccountId = message.DestinationAccountId,
                SourceAmount = message.SourceAmount,
                SourceCurrency = message.SourceCurrency,
                DestinationAmount = message.DestinationAmount,
                DestinationCurrency = message.DestinationCurrency
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new FxConversionRejected(message.ConversionId, exception.Message, DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
