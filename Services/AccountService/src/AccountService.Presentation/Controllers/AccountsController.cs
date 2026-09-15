using AccountService.Application.Commands.GetAccount;
using AccountService.Application.Commands.GetMyAccounts;
using Banking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AccountService.Presentation.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<GetMyAccountsResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetMyAccountsCommand
        {
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetAccountResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetAccountCommand
        {
            AccountId = id,
            RequestedCustomerId = customerId,
            CanReadAny = HasPermission(Permissions.CustomersRead)
        }, cancellationToken);

        return Ok(response);
    }

    private bool TryGetCustomerId(out Guid customerId)
    {
        return Guid.TryParse(User.FindFirstValue("customer_id"), out customerId) && customerId != Guid.Empty;
    }

    private bool HasPermission(string permission)
    {
        return User.Claims.Any(claim => claim.Type == "permission" && claim.Value == permission);
    }
}
