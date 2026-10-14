using Banking.Contracts.Events;
using LedgerService.Application.Commands.ApplyLoanDisbursement;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class LoanDisbursementRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<LoanDisbursementRequested>
{
    public async Task Consume(ConsumeContext<LoanDisbursementRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new ApplyLoanDisbursementCommand
            {
                LoanId = message.LoanId,
                AccountId = message.AccountId,
                Amount = message.Amount,
                Currency = message.Currency
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new LoanDisbursementRejected(message.LoanId, exception.Message, DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
