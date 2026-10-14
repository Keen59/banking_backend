using System.Security.Claims;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenBankingService.Application.Commands.CreateConsent;
using OpenBankingService.Application.Commands.GetAisAccount;
using OpenBankingService.Application.Commands.GetAisAccounts;
using OpenBankingService.Application.Commands.GetMyConsents;
using OpenBankingService.Application.Commands.GetPaymentInitiation;
using OpenBankingService.Application.Commands.InitiatePayment;
using OpenBankingService.Application.Commands.RevokeConsent;
using OpenBankingService.Presentation.Serialization;

namespace OpenBankingService.Presentation.Controllers;

[ApiController]
[Route("api/open-banking")]
[Authorize]
public class OpenBankingController(IMediator mediator) : ControllerBase
{
    [HttpPost("consents")]
    public async Task<ActionResult<CreateConsentResponse>> CreateConsent(
        [FromBody] CreateConsentRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { isSuccess = false, message = "Idempotency-Key header is required." });

        var response = await mediator.Send(new CreateConsentCommand
        {
            CustomerId = customerId,
            TppClientId = body.TppClientId,
            Scopes = body.Scopes,
            AccountIds = body.AccountIds,
            ExpiresInDays = body.ExpiresInDays <= 0 ? 90 : body.ExpiresInDays,
            IdempotencyKey = idempotencyKey.Trim()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("consents/me")]
    public async Task<ActionResult<GetMyConsentsResponse>> MyConsents(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetMyConsentsCommand
        {
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("consents/{id:guid}/revoke")]
    public async Task<ActionResult<RevokeConsentResponse>> Revoke(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new RevokeConsentCommand
        {
            ConsentId = id,
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("ais/accounts")]
    public async Task<ActionResult<GetAisAccountsResponse>> AisAccounts(CancellationToken cancellationToken)
    {
        if (!TryGetConsentToken(out var token))
            return Unauthorized();

        var response = await mediator.Send(new GetAisAccountsCommand
        {
            AccessToken = token
        }, cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("ais/accounts/{id:guid}")]
    public async Task<ActionResult<GetAisAccountResponse>> AisAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetConsentToken(out var token))
            return Unauthorized();

        var response = await mediator.Send(new GetAisAccountCommand
        {
            AccessToken = token,
            AccountId = id
        }, cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("pis/payments")]
    public async Task<ActionResult<InitiatePaymentResponse>> InitiatePayment(
        [FromBody] InitiatePaymentRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetConsentToken(out var token))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { isSuccess = false, message = "Idempotency-Key header is required." });

        var response = await mediator.Send(new InitiatePaymentCommand
        {
            AccessToken = token,
            SourceAccountId = body.SourceAccountId,
            DestinationAccountId = body.DestinationAccountId,
            Amount = body.Amount,
            Currency = body.Currency,
            Description = body.Description,
            IdempotencyKey = idempotencyKey.Trim()
        }, cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("pis/payments/{id:guid}")]
    public async Task<ActionResult<GetPaymentInitiationResponse>> GetPayment(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetConsentToken(out var token))
            return Unauthorized();

        var response = await mediator.Send(new GetPaymentInitiationCommand
        {
            AccessToken = token,
            InitiationId = id
        }, cancellationToken);

        return Ok(response);
    }

    private bool TryGetCustomerId(out Guid customerId)
    {
        return Guid.TryParse(User.FindFirstValue("customer_id"), out customerId) && customerId != Guid.Empty;
    }

    private bool TryGetConsentToken(out string token)
    {
        token = Request.Headers["X-Consent-Token"].ToString().Trim();
        return !string.IsNullOrWhiteSpace(token);
    }
}

public class CreateConsentRequest
{
    public string TppClientId { get; set; } = string.Empty;

    [JsonConverter(typeof(ConsentScopesJsonConverter))]
    public int Scopes { get; set; }

    public List<Guid> AccountIds { get; set; } = [];

    public int ExpiresInDays { get; set; } = 90;
}

public class InitiatePaymentRequest
{
    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;
}
