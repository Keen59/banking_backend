using Banking.Contracts.Events;
using MediatR;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Application.Mapping;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CreateTestCredit;

public class CreateTestCreditHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher,
    IOptions<TransferLimitOptions> limitOptions)
    : IRequestHandler<CreateTestCreditCommand, CreateTestCreditResponse>
{
    public async Task<CreateTestCreditResponse> Handle(
        CreateTestCreditCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.TestCredits.GetByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new CreateTestCreditResponse
            {
                Message = "Existing test credit returned for idempotency key.",
                Data = TestCreditMapper.ToDto(existing)
            };
        }

        var limits = limitOptions.Value;
        if (request.Amount > limits.MaxTestCredit)
            throw new InvalidOperationException($"Amount exceeds test-credit limit of {limits.MaxTestCredit}.");

        var account = await unitOfWork.AccountProjections.GetByIdAsync(request.AccountId);
        if (account is null)
            throw new InvalidOperationException("Account not found.");

        if (account.Status != EAccountProjectionStatus.Active)
            throw new InvalidOperationException("Account is not active.");

        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Account currency does not match the credit.");

        var now = DateTimeOffset.UtcNow;
        var credit = new TestCredit
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            AccountCustomerId = account.CustomerId,
            RequestedByCustomerId = request.RequestedByCustomerId,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            IdempotencyKey = request.IdempotencyKey,
            Status = ETransferStatus.Initiated,
            CreatedAt = now
        };

        await unitOfWork.TestCredits.AddAsync(credit, cancellationToken);
        await publisher.PublishAsync(
            new TestCreditRequested(
                credit.Id,
                credit.AccountId,
                credit.Amount,
                credit.Currency,
                now),
            cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new CreateTestCreditResponse
        {
            Message = "Test credit initiated.",
            Data = TestCreditMapper.ToDto(credit)
        };
    }
}
