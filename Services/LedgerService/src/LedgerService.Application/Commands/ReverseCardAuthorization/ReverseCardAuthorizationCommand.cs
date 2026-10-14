using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.ReverseCardAuthorization;

public class ReverseCardAuthorizationCommand : IRequest<ReverseCardAuthorizationResponse>
{
    public Guid AuthorizationId { get; init; }

    public string Reason { get; init; } = string.Empty;
}

public class ReverseCardAuthorizationResponse : Response
{
}
