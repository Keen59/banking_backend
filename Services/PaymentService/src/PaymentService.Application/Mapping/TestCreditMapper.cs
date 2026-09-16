using PaymentService.Application.DTOs.TestCredits;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Mapping;

public static class TestCreditMapper
{
    public static TestCreditDto ToDto(TestCredit credit)
    {
        return new TestCreditDto
        {
            Id = credit.Id,
            AccountId = credit.AccountId,
            AccountCustomerId = credit.AccountCustomerId,
            Amount = credit.Amount,
            Currency = credit.Currency,
            Status = credit.Status,
            RejectReason = credit.RejectReason,
            JournalEntryId = credit.JournalEntryId
        };
    }
}
