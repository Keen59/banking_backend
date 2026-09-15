using MediatR;

namespace CustomerService.Application.Commands.GetKycDocumentFile;

public class GetKycDocumentFileCommand : IRequest<GetKycDocumentFileResponse>
{
    public Guid CustomerId { get; init; }

    public Guid DocumentId { get; init; }

    public Guid RequestedCustomerId { get; init; }

    public bool CanReadAny { get; init; }
}
