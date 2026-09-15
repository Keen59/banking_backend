using Banking.Contracts.Authorization;
using CustomerService.Application.Commands.CreateCustomer;
using CustomerService.Application.Commands.GetCustomer;
using CustomerService.Application.Commands.GetKycDocumentFile;
using CustomerService.Application.Commands.GetPendingKyc;
using CustomerService.Application.Commands.ReviewKyc;
using CustomerService.Application.Commands.SubmitKyc;
using CustomerService.Application.Commands.UpdateCustomer;
using CustomerService.Application.Commands.UploadKycDocument;
using CustomerService.Presentation.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CustomerService.Presentation.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateCustomerResponse>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new CreateCustomerCommand
        {
            CustomerId = customerId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            NationalId = request.NationalId,
            DateOfBirth = request.DateOfBirth,
            Nationality = request.Nationality,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            City = request.City,
            District = request.District,
            Line1 = request.Line1,
            PostalCode = request.PostalCode,
            KvkkConsent = request.KvkkConsent,
            IpAddress = GetClientIpAddress()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("me")]
    public async Task<ActionResult<GetCustomerResponse>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetCustomerCommand
        {
            CustomerId = customerId,
            RequestedCustomerId = customerId
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("kyc/pending")]
    [Authorize(Policy = AuthorizationPolicies.KycReview)]
    public async Task<ActionResult<GetPendingKycResponse>> PendingKyc(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetPendingKycCommand(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetCustomerResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new GetCustomerCommand
        {
            CustomerId = id,
            RequestedCustomerId = customerId,
            CanReadAny = HasPermission(Permissions.CustomersRead)
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateCustomerResponse>> Update(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new UpdateCustomerCommand
        {
            CustomerId = id,
            RequestedCustomerId = customerId,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            City = request.City,
            District = request.District,
            Line1 = request.Line1,
            PostalCode = request.PostalCode,
            IpAddress = GetClientIpAddress()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/kyc/documents")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<UploadKycDocumentResponse>> UploadDocument(
        Guid id,
        [FromForm] UploadKycDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { isSuccess = false, message = "Dosya zorunludur." });

        await using var stream = request.File.OpenReadStream();
        var response = await mediator.Send(new UploadKycDocumentCommand
        {
            CustomerId = id,
            RequestedCustomerId = customerId,
            DocumentType = request.DocumentType,
            FileName = request.File.FileName,
            ContentType = request.File.ContentType,
            Content = stream,
            IpAddress = GetClientIpAddress()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}/kyc/documents/{documentId:guid}")]
    public async Task<IActionResult> DownloadDocument(
        Guid id,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var file = await mediator.Send(new GetKycDocumentFileCommand
        {
            CustomerId = id,
            DocumentId = documentId,
            RequestedCustomerId = customerId,
            CanReadAny = HasPermission(Permissions.CustomersRead) || HasPermission(Permissions.KycReview)
        }, cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpPost("{id:guid}/kyc/submit")]
    public async Task<ActionResult<SubmitKycResponse>> SubmitKyc(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerId))
            return Unauthorized();

        var response = await mediator.Send(new SubmitKycCommand
        {
            CustomerId = id,
            RequestedCustomerId = customerId,
            IpAddress = GetClientIpAddress()
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/kyc/review")]
    [Authorize(Policy = AuthorizationPolicies.KycReview)]
    public async Task<ActionResult<ReviewKycResponse>> ReviewKyc(
        Guid id,
        [FromBody] ReviewKycRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new ReviewKycCommand
        {
            CustomerId = id,
            Approved = request.Approved,
            RejectReason = request.RejectReason,
            IpAddress = GetClientIpAddress()
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

    private string GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(ip))
                return ip;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
