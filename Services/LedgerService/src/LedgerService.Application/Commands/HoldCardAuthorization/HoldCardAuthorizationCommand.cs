using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.HoldCardAuthorization;

public class HoldCardAuthorizationCommand : IRequest<HoldCardAuthorizationResponse>
{
    public Guid AuthorizationId { get; init; }

    public Guid AccountId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "TRY";

    public string Merchant { get; init; } = string.Empty;
}

public class HoldCardAuthorizationResponse : Response
{
    public Guid? JournalEntryId { get; set; }
}
