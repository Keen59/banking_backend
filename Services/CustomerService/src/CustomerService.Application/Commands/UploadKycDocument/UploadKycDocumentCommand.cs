using CustomerService.Domain.Enums;
using MediatR;

namespace CustomerService.Application.Commands.UploadKycDocument;

public class UploadKycDocumentCommand : IRequest<UploadKycDocumentResponse>
{
    public Guid CustomerId { get; init; }

    public Guid RequestedCustomerId { get; init; }

    public EDocumentType DocumentType { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public Stream Content { get; init; } = Stream.Null;

    public string? IpAddress { get; init; }
}
