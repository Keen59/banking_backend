using CustomerService.Domain.Enums;

namespace CustomerService.Application.DTOs.Customers;

public class CustomerDto
{
    public Guid Id { get; set; }

    public string CifNumber { get; set; } = string.Empty;

    public ECustomerType Type { get; set; }

    public ECustomerStatus Status { get; set; }

    public EKycStatus KycStatus { get; set; }

    public EKycLevel KycLevel { get; set; }

    public DateTimeOffset? KycExpiresAt { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? NationalId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string Nationality { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public IReadOnlyCollection<AddressDto> Addresses { get; set; } = [];

    public IReadOnlyCollection<DocumentDto> Documents { get; set; } = [];

    public IReadOnlyCollection<ConsentDto> Consents { get; set; } = [];
}

public class AddressDto
{
    public Guid Id { get; set; }

    public EAddressType Type { get; set; }

    public string Country { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string Line1 { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    public bool IsPrimary { get; set; }
}

public class DocumentDto
{
    public Guid Id { get; set; }

    public EDocumentType DocumentType { get; set; }

    public string FileReference { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public EDocumentStatus Status { get; set; }
}

public class ConsentDto
{
    public EConsentType Type { get; set; }

    public bool Granted { get; set; }

    public DateTimeOffset GrantedAt { get; set; }
}
