using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using PaymentService.Application.Commands.CreateOpenBankingPayment;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;

namespace PaymentService.Infrastructure.Consumers;

public sealed class OpenBankingPaymentRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<OpenBankingPaymentRequested>
{
    public async Task Consume(ConsumeContext<OpenBankingPaymentRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(
                new CreateOpenBankingPaymentCommand { Event = message },
                context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new OpenBankingPaymentRejected(message.InitiationId, exception.Message, DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
