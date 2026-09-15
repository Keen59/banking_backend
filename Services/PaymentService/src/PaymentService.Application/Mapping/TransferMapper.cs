using PaymentService.Application.DTOs.Transfers;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Mapping;

public static class TransferMapper
{
    public static TransferDto ToDto(Transfer transfer)
    {
        return new TransferDto
        {
            Id = transfer.Id,
            CustomerId = transfer.CustomerId,
            SourceAccountId = transfer.SourceAccountId,
            DestinationAccountId = transfer.DestinationAccountId,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            Description = transfer.Description,
            Status = transfer.Status,
            RejectReason = transfer.RejectReason,
            JournalEntryId = transfer.JournalEntryId
        };
    }
}
