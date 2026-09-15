using AccountService.Application.DTOs;
using AccountService.Application.DTOs.Accounts;

namespace AccountService.Application.Commands.GetMyAccounts;

public class GetMyAccountsResponse : Response
{
    public IReadOnlyCollection<AccountDto> Accounts { get; set; } = [];
}
