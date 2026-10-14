using PaymentService.Application.DTOs.FxConversions;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Mapping;

public static class FxConversionMapper
{
    public static FxConversionDto ToDto(FxConversion conversion)
    {
        return new FxConversionDto
        {
            Id = conversion.Id,
            CustomerId = conversion.CustomerId,
            SourceAccountId = conversion.SourceAccountId,
            DestinationAccountId = conversion.DestinationAccountId,
            SourceAmount = conversion.SourceAmount,
            SourceCurrency = conversion.SourceCurrency,
            DestinationAmount = conversion.DestinationAmount,
            DestinationCurrency = conversion.DestinationCurrency,
            Rate = conversion.Rate,
            TryEquivalent = conversion.TryEquivalent,
            Description = conversion.Description,
            Status = conversion.Status,
            RejectReason = conversion.RejectReason,
            SellJournalEntryId = conversion.SellJournalEntryId,
            BuyJournalEntryId = conversion.BuyJournalEntryId
        };
    }
}
