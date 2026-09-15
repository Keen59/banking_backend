using Banking.Contracts.Events;
using LedgerService.Application.Commands.ExecuteInternalTransfer;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class TransferRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<TransferRequested>
{
    public async Task Consume(ConsumeContext<TransferRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new ExecuteInternalTransferCommand
            {
                TransferId = message.TransferId,
                SourceAccountId = message.SourceAccountId,
                DestinationAccountId = message.DestinationAccountId,
                Amount = message.Amount,
                Currency = message.Currency,
                Description = message.Description
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new TransferRejected(message.TransferId, exception.Message, DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
