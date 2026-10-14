using Banking.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiskService.Application.Commands.CloseAlert;
using RiskService.Application.Commands.GetAlert;
using RiskService.Application.Commands.GetOpenAlerts;

namespace RiskService.Presentation.Controllers;

[ApiController]
[Route("api/risk/alerts")]
[Authorize]
public class RiskAlertsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RiskReview)]
    public async Task<ActionResult<GetOpenAlertsResponse>> GetOpen(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetOpenAlertsCommand(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RiskReview)]
    public async Task<ActionResult<GetAlertResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetAlertCommand
        {
            AlertId = id
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Policy = AuthorizationPolicies.RiskReview)]
    public async Task<ActionResult<CloseAlertResponse>> Close(
        Guid id,
        [FromBody] CloseAlertRequest body,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new CloseAlertCommand
        {
            AlertId = id,
            Reason = body.Reason
        }, cancellationToken);

        return Ok(response);
    }
}

public class CloseAlertRequest
{
    public string Reason { get; set; } = string.Empty;
}
