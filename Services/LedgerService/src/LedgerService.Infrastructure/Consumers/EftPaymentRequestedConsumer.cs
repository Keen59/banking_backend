using Banking.Contracts.Events;
using LedgerService.Application.Commands.HoldEftPayment;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class EftPaymentRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<EftPaymentRequested>
{
    public async Task Consume(ConsumeContext<EftPaymentRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new HoldEftPaymentCommand
            {
                EftPaymentId = message.EftPaymentId,
                SourceAccountId = message.SourceAccountId,
                Amount = message.Amount,
                Currency = message.Currency
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new EftPaymentRejected(message.EftPaymentId, exception.Message, DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
