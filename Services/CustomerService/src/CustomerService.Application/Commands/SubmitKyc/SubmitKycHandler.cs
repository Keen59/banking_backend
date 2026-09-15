using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Mapping;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Enums;
using MediatR;

namespace CustomerService.Application.Commands.SubmitKyc;

public class SubmitKycHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitKycCommand, SubmitKycResponse>
{
    public async Task<SubmitKycResponse> Handle(SubmitKycCommand request, CancellationToken cancellationToken)
    {
        if (request.RequestedCustomerId != request.CustomerId)
            throw new UnauthorizedAccessException("Bu müşteri kaydına erişim yetkiniz yok.");

        var customer = await unitOfWork.CustomerRepository.GetByIdWithDetailsAsync(
            request.CustomerId,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Müşteri bulunamadı.");

        if (string.IsNullOrWhiteSpace(customer.NationalId) ||
            !customer.Consents.Any(x => x.Type == EConsentType.Kvkk && x.Granted))
        {
            throw new InvalidOperationException("KYC gönderilmeden önce onboarding (TCKN ve KVKK) tamamlanmalıdır.");
        }

        if (customer.Status == ECustomerStatus.Closed)
            throw new InvalidOperationException("Kapatılmış müşteri için KYC gönderilemez.");

        if (customer.KycStatus == EKycStatus.Approved &&
            customer.KycExpiresAt is { } expiresAt &&
            expiresAt > DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Geçerli bir KYC kaydı zaten onaylanmış.");
        }

        var uploaded = customer.Documents.Where(x => x.Status == EDocumentStatus.Uploaded).ToList();
        if (!uploaded.Any(x => x.DocumentType is EDocumentType.IdentityCard or EDocumentType.Passport))
            throw new InvalidOperationException("Kimlik kartı veya pasaport belgesi yüklenmelidir.");

        customer.KycStatus = EKycStatus.Pending;
        customer.KycLevel = EKycLevel.Basic;
        customer.KycRejectReason = null;
        unitOfWork.CustomerRepository.Update(customer);

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            CustomerId = customer.Id,
            Action = EAuditAction.KycSubmitted,
            Resource = "Kyc",
            IpAddress = request.IpAddress
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new SubmitKycResponse
        {
            Message = "KYC belgeleriniz alındı. İnceleme bekleniyor.",
            Customer = CustomerMapper.ToDto(customer)
        };
    }
}
