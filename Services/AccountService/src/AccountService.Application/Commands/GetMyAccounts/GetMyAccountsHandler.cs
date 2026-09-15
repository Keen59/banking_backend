using AccountService.Application.Interfaces.Repositories;
using AccountService.Application.Mapping;
using MediatR;

namespace AccountService.Application.Commands.GetMyAccounts;

public class GetMyAccountsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyAccountsCommand, GetMyAccountsResponse>
{
    public async Task<GetMyAccountsResponse> Handle(GetMyAccountsCommand request, CancellationToken cancellationToken)
    {
        var accounts = await unitOfWork.AccountRepository.GetByCustomerIdAsync(
            request.CustomerId,
            cancellationToken);

        return new GetMyAccountsResponse
        {
            Message = "Hesap listesi.",
            Accounts = accounts.Select(AccountMapper.ToDto).ToList()
        };
    }
}
