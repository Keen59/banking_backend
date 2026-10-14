using Banking.Contracts.Events;
using LedgerService.Application.Commands.AcceptIncomingEftPayment;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class IncomingEftPaymentRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<IncomingEftPaymentRequested>
{
    public async Task Consume(ConsumeContext<IncomingEftPaymentRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new AcceptIncomingEftPaymentCommand
            {
                IncomingEftPaymentId = message.IncomingEftPaymentId,
                DestinationAccountId = message.DestinationAccountId,
                Amount = message.Amount,
                Currency = message.Currency
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new IncomingEftPaymentRejected(
                    message.IncomingEftPaymentId,
                    exception.Message,
                    DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
