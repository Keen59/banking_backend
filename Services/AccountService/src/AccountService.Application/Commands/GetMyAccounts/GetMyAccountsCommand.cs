using MediatR;

namespace AccountService.Application.Commands.GetMyAccounts;

public class GetMyAccountsCommand : IRequest<GetMyAccountsResponse>
{
    public Guid CustomerId { get; init; }
}
