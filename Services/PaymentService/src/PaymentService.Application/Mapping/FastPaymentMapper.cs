using PaymentService.Application.DTOs.FastPayments;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Mapping;

public static class FastPaymentMapper
{
    public static FastPaymentDto ToDto(FastPayment payment)
    {
        return new FastPaymentDto
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
