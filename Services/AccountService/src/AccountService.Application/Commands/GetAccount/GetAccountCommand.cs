using MediatR;

namespace AccountService.Application.Commands.GetAccount;

public class GetAccountCommand : IRequest<GetAccountResponse>
{
    public Guid AccountId { get; init; }

    public Guid RequestedCustomerId { get; init; }

    public bool CanReadAny { get; init; }
}
