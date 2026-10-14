using AccountService.Application.DTOs;
using AccountService.Application.DTOs.Accounts;
using MediatR;

namespace AccountService.Application.Commands.OpenFxAccount;

public class OpenFxAccountCommand : IRequest<OpenFxAccountResponse>
{
    public Guid CustomerId { get; set; }

    public string Currency { get; set; } = string.Empty;
}

public class OpenFxAccountResponse : Response
{
    public AccountDto? Account { get; set; }
}
