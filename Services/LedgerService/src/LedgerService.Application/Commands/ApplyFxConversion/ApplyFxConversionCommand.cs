using LedgerService.Application.DTOs;
using MediatR;

namespace LedgerService.Application.Commands.ApplyFxConversion;

public class ApplyFxConversionCommand : IRequest<ApplyFxConversionResponse>
{
    public Guid ConversionId { get; init; }

    public Guid SourceAccountId { get; init; }

    public Guid DestinationAccountId { get; init; }

    public decimal SourceAmount { get; init; }

    public string SourceCurrency { get; init; } = "TRY";

    public decimal DestinationAmount { get; init; }

    public string DestinationCurrency { get; init; } = "USD";
}

public class ApplyFxConversionResponse : Response
{
    public Guid SellJournalEntryId { get; set; }

    public Guid BuyJournalEntryId { get; set; }
}
