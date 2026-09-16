using PaymentService.Application.DTOs.IncomingFastPayments;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Mapping;

public static class IncomingFastPaymentMapper
{
    public static IncomingFastPaymentDto ToDto(IncomingFastPayment payment)
    {
        return new IncomingFastPaymentDto
        {
            Id = payment.Id,
            AccountId = payment.AccountId,
            AccountCustomerId = payment.AccountCustomerId,
            DestinationIban = payment.DestinationIban,
            SourceIban = payment.SourceIban,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Description = payment.Description,
            Status = payment.Status,
            RejectReason = payment.RejectReason,
            JournalEntryId = payment.JournalEntryId
        };
    }
}
