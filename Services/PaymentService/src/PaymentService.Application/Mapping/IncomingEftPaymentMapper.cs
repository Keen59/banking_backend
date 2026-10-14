using PaymentService.Application.DTOs.IncomingEftPayments;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Mapping;

public static class IncomingEftPaymentMapper
{
    public static IncomingEftPaymentDto ToDto(IncomingEftPayment payment)
    {
        return new IncomingEftPaymentDto
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
