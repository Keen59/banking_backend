using CustomerService.Domain.Enums;

namespace CustomerService.Domain.Entities;

public class Customer : BaseEntity
{
    public string CifNumber { get; set; } = null!;

    public ECustomerType Type { get; set; }

    public ECustomerStatus Status { get; set; }

    public EKycStatus KycStatus { get; set; }

    public EKycLevel KycLevel { get; set; }

    public DateTimeOffset? KycReviewedAt { get; set; }

    public DateTimeOffset? KycExpiresAt { get; set; }

    public string? KycRejectReason { get; set; }

    public string? NationalId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string Nationality { get; set; } = "TR";

    public string Email { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public ICollection<CustomerAddress> Addresses { get; set; } = [];

    public ICollection<KycDocument> Documents { get; set; } = [];

    public ICollection<CustomerConsent> Consents { get; set; } = [];

    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
