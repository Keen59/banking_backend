using AccountService.Application.DTOs;
using AccountService.Application.DTOs.Accounts;

namespace AccountService.Application.Commands.GetAccount;

public class GetAccountResponse : Response
{
    public AccountDto? Account { get; set; }
}
