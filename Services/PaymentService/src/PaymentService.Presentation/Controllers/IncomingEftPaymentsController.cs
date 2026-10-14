using Banking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands.CreateIncomingEftPayment;
using PaymentService.Application.Commands.GetIncomingEftPayment;
using PaymentService.Application.Commands.GetMyIncomingEftPayments;
using PaymentService.Application.Commands.RequestIncomingEftReturn;
using PaymentService.Application.Commands.RequestIncomingEftSettlement;
using System.Security.Claims;

namespace PaymentService.Presentation.Controllers;

[ApiController]
[Route("api/payments/incoming-eft")]
[Authorize]
public class IncomingEftPaymentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PaymentsCredit)]
    public async Task<ActionResult<CreateIncomingEftPaymentResponse>> Create(
        [FromBody] CreateIncomingEftPaymentRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { isSuccess = false, message = "Idempotency-Key header is required." });

        var response = await mediator.Send(new CreateIncomingEftPaymentCommand
        {
            RequestedByCustomerId = customerId,
            DestinationIban = body.DestinationIban,
            SourceIban = body.SourceIban,
            Amount = body.Amount,
            Currency = body.Currency,
            Description = body.Description,
            IdempotencyKey = idempotencyKey.Trim()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("me")]
    public async Task<ActionResult<GetMyIncomingEftPaymentsResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetMyIncomingEftPaymentsCommand
        {
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetIncomingEftPaymentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetIncomingEftPaymentCommand
        {
            IncomingEftPaymentId = id,
            CustomerId = customerId,
            CanReadAny = HasPermission(Permissions.CustomersRead)
                         || HasPermission(Permissions.PaymentsCredit)
                         || HasPermission(Permissions.PaymentsSettle)
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/settle")]
    [Authorize(Policy = AuthorizationPolicies.PaymentsSettle)]
    public async Task<ActionResult<RequestIncomingEftSettlementResponse>> Settle(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new RequestIncomingEftSettlementCommand
        {
            IncomingEftPaymentId = id
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicies.PaymentsSettle)]
    public async Task<ActionResult<RequestIncomingEftReturnResponse>> Reject(
        Guid id,
        [FromBody] RejectIncomingEftPaymentRequest? body,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new RequestIncomingEftReturnCommand
        {
            IncomingEftPaymentId = id,
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

public class CreateIncomingEftPaymentRequest
{
    public string DestinationIban { get; set; } = string.Empty;

    public string SourceIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;
}

public class RejectIncomingEftPaymentRequest
{
    public string Reason { get; set; } = string.Empty;
}
