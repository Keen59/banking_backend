using Banking.Contracts.Events;
using LedgerService.Application.Commands.ExecuteFastPayment;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class FastPaymentRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<FastPaymentRequested>
{
    public async Task Consume(ConsumeContext<FastPaymentRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new ExecuteFastPaymentCommand
            {
                FastPaymentId = message.FastPaymentId,
                SourceAccountId = message.SourceAccountId,
                DestinationIban = message.DestinationIban,
                Amount = message.Amount,
                Currency = message.Currency,
                Description = message.Description
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new FastPaymentRejected(message.FastPaymentId, exception.Message, DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
