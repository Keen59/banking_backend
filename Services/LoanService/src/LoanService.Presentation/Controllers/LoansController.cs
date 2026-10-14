using Banking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoanService.Application.Commands.ApplyLoan;
using LoanService.Application.Commands.ApproveLoan;
using LoanService.Application.Commands.GetLoan;
using LoanService.Application.Commands.GetMyLoans;
using LoanService.Application.Commands.RejectLoan;
using LoanService.Application.Commands.RequestRepayment;
using System.Security.Claims;

namespace LoanService.Presentation.Controllers;

[ApiController]
[Route("api/loans")]
[Authorize]
public class LoansController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApplyLoanResponse>> Apply(
        [FromBody] ApplyLoanRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { isSuccess = false, message = "Idempotency-Key header is required." });

        var response = await mediator.Send(new ApplyLoanCommand
        {
            CustomerId = customerId,
            AccountId = body.AccountId,
            Amount = body.Amount,
            TermMonths = body.TermMonths,
            Currency = body.Currency,
            IdempotencyKey = idempotencyKey.Trim()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("me")]
    public async Task<ActionResult<GetMyLoansResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetMyLoansCommand
        {
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetLoanResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetLoanCommand
        {
            LoanId = id,
            CustomerId = customerId,
            CanReadAny = HasPermission(Permissions.CustomersRead)
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicies.LoansReview)]
    public async Task<ActionResult<ApproveLoanResponse>> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new ApproveLoanCommand
        {
            LoanId = id
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.LoansReview)]
    public async Task<ActionResult<RejectLoanResponse>> Reject(
        Guid id,
        [FromBody] RejectLoanRequest? body,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new RejectLoanCommand
        {
            LoanId = id,
            Reason = body?.Reason ?? string.Empty
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/repay")]
    public async Task<ActionResult<RequestRepaymentResponse>> Repay(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new RequestRepaymentCommand
        {
            CustomerId = customerId,
            LoanId = id
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

public class ApplyLoanRequest
{
    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public int TermMonths { get; set; }

    public string Currency { get; set; } = "TRY";
}

public class RejectLoanRequest
{
    public string Reason { get; set; } = string.Empty;
}
