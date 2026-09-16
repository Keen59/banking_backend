using Banking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands.CreateFastPayment;
using PaymentService.Application.Commands.GetFastPayment;
using PaymentService.Application.Commands.GetMyFastPayments;
using System.Security.Claims;

namespace PaymentService.Presentation.Controllers;

[ApiController]
[Route("api/payments/fast")]
[Authorize]
public class FastPaymentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateFastPaymentResponse>> Create(
        [FromBody] CreateFastPaymentRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { isSuccess = false, message = "Idempotency-Key header is required." });

        var response = await mediator.Send(new CreateFastPaymentCommand
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
    public async Task<ActionResult<GetMyFastPaymentsResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetMyFastPaymentsCommand
        {
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetFastPaymentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetFastPaymentCommand
        {
            FastPaymentId = id,
            CustomerId = customerId,
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

public class CreateFastPaymentRequest
{
    public Guid SourceAccountId { get; set; }

    public string DestinationIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;
}
