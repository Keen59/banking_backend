using Banking.Contracts.Events;
using LedgerService.Application.Commands.ApplyTestCredit;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class TestCreditRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<TestCreditRequested>
{
    public async Task Consume(ConsumeContext<TestCreditRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new ApplyTestCreditCommand
            {
                CreditId = message.CreditId,
                AccountId = message.AccountId,
                Amount = message.Amount,
                Currency = message.Currency
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new TestCreditRejected(message.CreditId, exception.Message, DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
