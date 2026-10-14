using Banking.Contracts.Authorization;
using CardService.Application.Commands.AuthorizeCard;
using CardService.Application.Commands.CaptureCardAuthorization;
using CardService.Application.Commands.FreezeCard;
using CardService.Application.Commands.GetCard;
using CardService.Application.Commands.GetCardAuthorizations;
using CardService.Application.Commands.GetMyCards;
using CardService.Application.Commands.ReverseCardAuthorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CardService.Presentation.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public class CardsController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<GetMyCardsResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetMyCardsCommand
        {
            CustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetCardResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetCardCommand
        {
            CardId = id,
            CustomerId = customerId,
            CanReadAny = HasPermission(Permissions.CustomersRead)
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/freeze")]
    public async Task<ActionResult<FreezeCardResponse>> Freeze(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new FreezeCardCommand
        {
            CustomerId = customerId,
            CardId = id
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/authorize")]
    public async Task<ActionResult<AuthorizeCardResponse>> Authorize(
        Guid id,
        [FromBody] AuthorizeCardRequest body,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { isSuccess = false, message = "Idempotency-Key header is required." });

        var response = await mediator.Send(new AuthorizeCardCommand
        {
            CustomerId = customerId,
            CardId = id,
            Amount = body.Amount,
            Currency = body.Currency,
            Merchant = body.Merchant,
            Description = body.Description,
            IdempotencyKey = idempotencyKey.Trim()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/authorizations/{authId:guid}/capture")]
    public async Task<ActionResult<CaptureCardAuthorizationResponse>> Capture(
        Guid id,
        Guid authId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new CaptureCardAuthorizationCommand
        {
            CustomerId = customerId,
            CardId = id,
            AuthorizationId = authId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/authorizations/{authId:guid}/reverse")]
    public async Task<ActionResult<ReverseCardAuthorizationResponse>> Reverse(
        Guid id,
        Guid authId,
        [FromBody] ReverseCardAuthorizationRequest? body,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new ReverseCardAuthorizationCommand
        {
            CustomerId = customerId,
            CardId = id,
            AuthorizationId = authId,
            Reason = body?.Reason ?? string.Empty
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}/authorizations")]
    public async Task<ActionResult<GetCardAuthorizationsResponse>> Authorizations(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetCardAuthorizationsCommand
        {
            CustomerId = customerId,
            CardId = id
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

public class AuthorizeCardRequest
{
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Merchant { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public class ReverseCardAuthorizationRequest
{
    public string Reason { get; set; } = string.Empty;
}
