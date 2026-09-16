using PaymentService.Application.DTOs.EftPayments;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Mapping;

public static class EftPaymentMapper
{
    public static EftPaymentDto ToDto(EftPayment payment)
    {
        return new EftPaymentDto
        {
            Id = payment.Id,
            CustomerId = payment.CustomerId,
            SourceAccountId = payment.SourceAccountId,
            DestinationIban = payment.DestinationIban,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Description = payment.Description,
            Status = payment.Status,
            RejectReason = payment.RejectReason,
            JournalEntryId = payment.JournalEntryId
        };
    }
}
