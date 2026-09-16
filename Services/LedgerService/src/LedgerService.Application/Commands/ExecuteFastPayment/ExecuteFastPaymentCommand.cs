using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.ExecuteFastPayment;

public class ExecuteFastPaymentCommand : IRequest<ExecuteFastPaymentResponse>
{
    public Guid FastPaymentId { get; init; }

    public Guid SourceAccountId { get; init; }

    public string DestinationIban { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "TRY";

    public string Description { get; init; } = string.Empty;
}

public class ExecuteFastPaymentResponse : Response
{
    public Guid JournalEntryId { get; set; }
}
