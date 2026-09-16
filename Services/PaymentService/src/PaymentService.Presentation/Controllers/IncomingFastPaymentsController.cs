using Banking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands.CreateIncomingFastPayment;
using PaymentService.Application.Commands.GetIncomingFastPayment;
using PaymentService.Application.Commands.GetMyIncomingFastPayments;
using System.Security.Claims;

namespace PaymentService.Presentation.Controllers;

[ApiController]
[Route("api/payments/incoming-fast")]
[Authorize]
public class IncomingFastPaymentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PaymentsCredit)]
    public async Task<ActionResult<CreateIncomingFastPaymentResponse>> Create(
        [FromBody] CreateIncomingFastPaymentRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { isSuccess = false, message = "Idempotency-Key header is required." });

        var response = await mediator.Send(new CreateIncomingFastPaymentCommand
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
    public async Task<ActionResult<GetMyIncomingFastPaymentsResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetMyIncomingFastPaymentsCommand
        {
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetIncomingFastPaymentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetIncomingFastPaymentCommand
        {
            IncomingFastPaymentId = id,
            CustomerId = customerId,
            CanReadAny = HasPermission(Permissions.CustomersRead) || HasPermission(Permissions.PaymentsCredit)
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

public class CreateIncomingFastPaymentRequest
{
    public string DestinationIban { get; set; } = string.Empty;

    public string SourceIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;
}
