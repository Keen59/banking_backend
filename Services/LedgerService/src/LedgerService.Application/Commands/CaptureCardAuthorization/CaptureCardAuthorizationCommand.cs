using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.CaptureCardAuthorization;

public class CaptureCardAuthorizationCommand : IRequest<CaptureCardAuthorizationResponse>
{
    public Guid AuthorizationId { get; init; }

    public string Description { get; init; } = string.Empty;
}

public class CaptureCardAuthorizationResponse : Response
{
    public Guid? JournalEntryId { get; set; }
}
