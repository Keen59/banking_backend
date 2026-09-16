using Banking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands.CreateEftPayment;
using PaymentService.Application.Commands.GetEftPayment;
using PaymentService.Application.Commands.GetMyEftPayments;
using PaymentService.Application.Commands.RequestEftReturn;
using PaymentService.Application.Commands.RequestEftSettlement;
using System.Security.Claims;

namespace PaymentService.Presentation.Controllers;

[ApiController]
[Route("api/payments/eft")]
[Authorize]
public class EftPaymentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateEftPaymentResponse>> Create(
        [FromBody] CreateEftPaymentRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { isSuccess = false, message = "Idempotency-Key header is required." });

        var response = await mediator.Send(new CreateEftPaymentCommand
        {
            CustomerId = customerId,
            SourceAccountId = body.SourceAccountId,
            DestinationIban = body.DestinationIban,
            Amount = body.Amount,
            Currency = body.Currency,
            Description = body.Description,
            IdempotencyKey = idempotencyKey.Trim()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("me")]
    public async Task<ActionResult<GetMyEftPaymentsResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetMyEftPaymentsCommand
        {
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetEftPaymentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetEftPaymentCommand
        {
            EftPaymentId = id,
            CustomerId = customerId,
            CanReadAny = HasPermission(Permissions.CustomersRead) || HasPermission(Permissions.PaymentsSettle)
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/settle")]
    [Authorize(Policy = AuthorizationPolicies.PaymentsSettle)]
    public async Task<ActionResult<RequestEftSettlementResponse>> Settle(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new RequestEftSettlementCommand
        {
            EftPaymentId = id
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.PaymentsSettle)]
    public async Task<ActionResult<RequestEftReturnResponse>> Reject(
        Guid id,
        [FromBody] RejectEftPaymentRequest? body,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new RequestEftReturnCommand
        {
            EftPaymentId = id,
            Reason = body?.Reason ?? string.Empty
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

public class CreateEftPaymentRequest
{
    public Guid SourceAccountId { get; set; }

    public string DestinationIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;
}

public class RejectEftPaymentRequest
{
    public string Reason { get; set; } = string.Empty;
}
