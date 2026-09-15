using Banking.Contracts.Authorization;
using LedgerService.Application.Commands.GetAccountBalance;
using LedgerService.Application.Commands.GetAccountMovements;
using LedgerService.Application.Commands.GetMyBalances;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LedgerService.Presentation.Controllers;

[ApiController]
[Route("api/ledger")]
[Authorize]
public class LedgerController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<GetMyBalancesResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetMyBalancesCommand
        {
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("accounts/{accountId:guid}")]
    public async Task<ActionResult<GetAccountBalanceResponse>> GetBalance(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetAccountBalanceCommand
        {
            AccountId = accountId,
            RequestedCustomerId = customerId,
            CanReadAny = HasPermission(Permissions.CustomersRead)
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("accounts/{accountId:guid}/movements")]
    public async Task<ActionResult<GetAccountMovementsResponse>> GetMovements(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetAccountMovementsCommand
        {
            AccountId = accountId,
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
