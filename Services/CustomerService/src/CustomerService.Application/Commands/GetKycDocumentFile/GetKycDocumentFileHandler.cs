using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Interfaces.Services;
using MediatR;

namespace CustomerService.Application.Commands.GetKycDocumentFile;

public class GetKycDocumentFileHandler(
    IUnitOfWork unitOfWork,
    IKycFileStorage fileStorage) : IRequestHandler<GetKycDocumentFileCommand, GetKycDocumentFileResponse>
{
    public async Task<GetKycDocumentFileResponse> Handle(
        GetKycDocumentFileCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.CanReadAny && request.RequestedCustomerId != request.CustomerId)
            throw new UnauthorizedAccessException("Bu belgeye erişim yetkiniz yok.");

        var customer = await unitOfWork.CustomerRepository.GetByIdWithDetailsAsync(
            request.CustomerId,
            asNoTracking: true,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Müşteri bulunamadı.");

        var document = customer.Documents.FirstOrDefault(x => x.Id == request.DocumentId)
            ?? throw new InvalidOperationException("Belge bulunamadı.");

        var stream = await fileStorage.OpenReadAsync(document.FileReference, cancellationToken);

        return new GetKycDocumentFileResponse
        {
            FileName = document.OriginalFileName,
            ContentType = document.ContentType,
            Content = stream
        };
    }
}
