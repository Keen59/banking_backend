using LoanService.Application.DTOs.Loans;
using LoanService.Domain.Entities;

namespace LoanService.Application.Mapping;

public static class LoanMapper
{
    public static LoanDto ToDto(Loan loan)
    {
        return new LoanDto
        {
            Id = loan.Id,
            CustomerId = loan.CustomerId,
            AccountId = loan.AccountId,
            Amount = loan.Amount,
            Currency = loan.Currency,
            TermMonths = loan.TermMonths,
            RemainingPrincipal = loan.RemainingPrincipal,
            Status = loan.Status,
            RejectReason = loan.RejectReason,
            JournalEntryId = loan.JournalEntryId,
            CreatedAt = loan.CreatedAt,
            Installments = loan.Installments
                .OrderBy(x => x.Sequence)
                .Select(ToDto)
                .ToList()
        };
    }

    public static LoanInstallmentDto ToDto(LoanInstallment installment)
    {
        return new LoanInstallmentDto
        {
            Id = installment.Id,
            Sequence = installment.Sequence,
            DueDate = installment.DueDate,
            Amount = installment.Amount,
            Status = installment.Status,
            PaidAt = installment.PaidAt,
            JournalEntryId = installment.JournalEntryId
        };
    }
}
