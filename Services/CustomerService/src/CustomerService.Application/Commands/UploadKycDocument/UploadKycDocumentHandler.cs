using CustomerService.Application.DTOs.Customers;
using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Interfaces.Services;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Enums;
using MediatR;

namespace CustomerService.Application.Commands.UploadKycDocument;

public class UploadKycDocumentHandler(
    IUnitOfWork unitOfWork,
    IKycFileStorage fileStorage) : IRequestHandler<UploadKycDocumentCommand, UploadKycDocumentResponse>
{
    public async Task<UploadKycDocumentResponse> Handle(
        UploadKycDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RequestedCustomerId != request.CustomerId)
            throw new UnauthorizedAccessException("Bu müşteri kaydına erişim yetkiniz yok.");

        var customer = await unitOfWork.CustomerRepository.GetByIdWithDetailsAsync(
            request.CustomerId,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Müşteri bulunamadı.");

        if (string.IsNullOrWhiteSpace(customer.NationalId))
            throw new InvalidOperationException("KYC belgesi yüklemeden önce onboarding tamamlanmalıdır.");

        if (customer.Status == ECustomerStatus.Closed)
            throw new InvalidOperationException("Kapatılmış müşteri için belge yüklenemez.");

        if (customer.KycStatus == EKycStatus.Approved &&
            customer.KycExpiresAt is { } expiresAt &&
            expiresAt > DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Geçerli bir KYC kaydı zaten onaylanmış.");
        }

        var stored = await fileStorage.SaveAsync(
            customer.Id,
            request.FileName,
            request.ContentType,
            request.Content,
            cancellationToken);

        var document = new KycDocument
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            DocumentType = request.DocumentType,
            FileReference = stored.FileReference,
            OriginalFileName = stored.OriginalFileName,
            ContentType = stored.ContentType,
            SizeBytes = stored.SizeBytes,
            Status = EDocumentStatus.Uploaded
        };

        customer.Documents.Add(document);
        unitOfWork.CustomerRepository.Update(customer);
        await unitOfWork.SaveAsync(cancellationToken);

        return new UploadKycDocumentResponse
        {
            Message = "Belge yüklendi.",
            Document = new DocumentDto
            {
                Id = document.Id,
                DocumentType = document.DocumentType,
                FileReference = document.FileReference,
                OriginalFileName = document.OriginalFileName,
                ContentType = document.ContentType,
                SizeBytes = document.SizeBytes,
                Status = document.Status
            }
        };
    }
}
