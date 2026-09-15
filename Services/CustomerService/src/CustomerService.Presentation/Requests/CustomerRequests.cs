using CustomerService.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace CustomerService.Presentation.Requests;

public class CreateCustomerRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string NationalId { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string Nationality { get; set; } = "TR";

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string Line1 { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    public bool KvkkConsent { get; set; }
}

public class UpdateCustomerRequest
{
    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Line1 { get; set; }

    public string? PostalCode { get; set; }
}

public class UploadKycDocumentRequest
{
    public EDocumentType DocumentType { get; set; }

    public IFormFile? File { get; set; }
}

public class ReviewKycRequest
{
    public bool Approved { get; set; }

    public string? RejectReason { get; set; }
}
