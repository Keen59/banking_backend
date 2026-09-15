using CustomerService.Domain.Enums;

namespace CustomerService.Domain.Entities;

public class KycDocument : BaseEntity
{
    public Guid CustomerId { get; set; }

    public EDocumentType DocumentType { get; set; }

    public string FileReference { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    public EDocumentStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Customer Customer { get; set; } = null!;
}
