using Banking.Contracts.Events;
using LedgerService.Application.Commands.ApplyLoanRepayment;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class LoanRepaymentRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<LoanRepaymentRequested>
{
    public async Task Consume(ConsumeContext<LoanRepaymentRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new ApplyLoanRepaymentCommand
            {
                LoanId = message.LoanId,
                InstallmentId = message.InstallmentId,
                AccountId = message.AccountId,
                Amount = message.Amount,
                Currency = message.Currency
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new LoanRepaymentRejected(
                    message.LoanId,
                    message.InstallmentId,
                    exception.Message,
                    DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
