using MediatR;

namespace LedgerService.Application.Commands.ExecuteInternalTransfer;

public class ExecuteInternalTransferCommand : IRequest<ExecuteInternalTransferResponse>
{
    public Guid TransferId { get; init; }

    public Guid SourceAccountId { get; init; }

    public Guid DestinationAccountId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "TRY";

    public string Description { get; init; } = string.Empty;
}
