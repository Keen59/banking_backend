using Banking.Contracts.Events;
using LedgerService.Application.Commands.ApplyIncomingFastPayment;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class IncomingFastPaymentRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<IncomingFastPaymentRequested>
{
    public async Task Consume(ConsumeContext<IncomingFastPaymentRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new ApplyIncomingFastPaymentCommand
            {
                IncomingFastPaymentId = message.IncomingFastPaymentId,
                DestinationAccountId = message.DestinationAccountId,
                Amount = message.Amount,
                Currency = message.Currency,
                Description = message.Description
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new IncomingFastPaymentRejected(
                    message.IncomingFastPaymentId,
                    exception.Message,
                    DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
