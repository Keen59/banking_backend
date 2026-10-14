using MediatR;
using Microsoft.Extensions.Options;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Mapping;
using LoanService.Application.Options;
using LoanService.Domain.Entities;
using LoanService.Domain.Enums;

namespace LoanService.Application.Commands.ApplyLoan;

public class ApplyLoanHandler(
    IUnitOfWork unitOfWork,
    IOptions<LoanLimitOptions> limitOptions)
    : IRequestHandler<ApplyLoanCommand, ApplyLoanResponse>
{
    public async Task<ApplyLoanResponse> Handle(
        ApplyLoanCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.Loans.GetByIdempotencyKeyAsync(
            request.CustomerId,
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new ApplyLoanResponse
            {
                Message = "Existing loan returned for idempotency key.",
                Data = LoanMapper.ToDto(existing)
            };
        }

        if (request.TermMonths < 3 || request.TermMonths > 12)
            throw new InvalidOperationException("TermMonths must be between 3 and 12.");

        var limits = limitOptions.Value;
        if (request.Amount > limits.MaxAmount)
            throw new InvalidOperationException($"Amount exceeds loan limit of {limits.MaxAmount}.");

        var account = await unitOfWork.AccountProjections.GetByIdAsync(request.AccountId);
        if (account is null)
            throw new InvalidOperationException("Account not found.");

        if (account.CustomerId != request.CustomerId)
            throw new InvalidOperationException("Account does not belong to the caller.");

        if (account.Status != EAccountProjectionStatus.Active)
            throw new InvalidOperationException("Account is not active.");

        if (!string.Equals(account.Currency, "TRY", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Account currency must be TRY.");

        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Account currency does not match the loan.");

        var now = DateTimeOffset.UtcNow;
        var loan = new Loan
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            AccountId = request.AccountId,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            TermMonths = request.TermMonths,
            RemainingPrincipal = 0m,
            IdempotencyKey = request.IdempotencyKey,
            Status = ELoanStatus.Applied,
            CreatedAt = now
        };

        await unitOfWork.Loans.AddAsync(loan, cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ApplyLoanResponse
        {
            Message = "Loan application submitted.",
            Data = LoanMapper.ToDto(loan)
        };
    }
}
