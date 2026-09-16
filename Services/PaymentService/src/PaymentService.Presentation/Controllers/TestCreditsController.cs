using Banking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands.CreateTestCredit;
using PaymentService.Application.Commands.GetTestCredit;
using System.Security.Claims;

namespace PaymentService.Presentation.Controllers;

[ApiController]
[Route("api/payments/test-credits")]
[Authorize]
public class TestCreditsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PaymentsCredit)]
    public async Task<ActionResult<CreateTestCreditResponse>> Create(
        [FromBody] CreateTestCreditRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { isSuccess = false, message = "Idempotency-Key header is required." });

        var response = await mediator.Send(new CreateTestCreditCommand
        {
            RequestedByCustomerId = customerId,
            AccountId = body.AccountId,
            Amount = body.Amount,
            Currency = body.Currency,
            IdempotencyKey = idempotencyKey.Trim()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.PaymentsCredit)]
    public async Task<ActionResult<GetTestCreditResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetTestCreditCommand
        {
            CreditId = id
        }, cancellationToken);

        return Ok(response);
    }

    private bool TryGetCustomerId(out Guid customerId)
    {
        return Guid.TryParse(User.FindFirstValue("customer_id"), out customerId) && customerId != Guid.Empty;
    }
}

public class CreateTestCreditRequest
{
    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";
}
