using MediatR;

namespace LedgerService.Application.Commands.GetAccountStatement;

public class GetAccountStatementCommand : IRequest<GetAccountStatementResponse>
{
    public Guid AccountId { get; init; }

    public Guid RequestedCustomerId { get; init; }

    public bool CanReadAny { get; init; }

    public DateTimeOffset From { get; init; }

    public DateTimeOffset To { get; init; }
}
