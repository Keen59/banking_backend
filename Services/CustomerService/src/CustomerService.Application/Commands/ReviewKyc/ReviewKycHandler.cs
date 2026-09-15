using Banking.Contracts.Events;
using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Interfaces.Services;
using CustomerService.Application.Mapping;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Enums;
using MediatR;

namespace CustomerService.Application.Commands.ReviewKyc;

public class ReviewKycHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher) : IRequestHandler<ReviewKycCommand, ReviewKycResponse>
{
    private static readonly TimeSpan KycValidity = TimeSpan.FromDays(365 * 2);

    public async Task<ReviewKycResponse> Handle(ReviewKycCommand request, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.CustomerRepository.GetByIdWithDetailsAsync(
            request.CustomerId,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Müşteri bulunamadı.");

        if (string.IsNullOrWhiteSpace(customer.NationalId))
            throw new InvalidOperationException("Onboarding tamamlanmamış müşteri onaylanamaz.");

        if (customer.KycStatus is not EKycStatus.Pending and not EKycStatus.InReview)
            throw new InvalidOperationException("Bu KYC kaydı inceleme için uygun değil.");

        var hasIdentity = customer.Documents.Any(x =>
            x.Status == EDocumentStatus.Uploaded &&
            x.DocumentType is EDocumentType.IdentityCard or EDocumentType.Passport);

        if (request.Approved && !hasIdentity)
            throw new InvalidOperationException("Kimlik belgesi olmadan KYC onaylanamaz.");

        if (request.Approved)
        {
            customer.KycStatus = EKycStatus.Approved;
            customer.KycLevel = EKycLevel.Full;
            customer.Status = ECustomerStatus.Active;
            customer.KycReviewedAt = DateTimeOffset.UtcNow;
            customer.KycExpiresAt = DateTimeOffset.UtcNow.Add(KycValidity);
            customer.KycRejectReason = null;

            foreach (var document in customer.Documents.Where(x => x.Status == EDocumentStatus.Uploaded))
                document.Status = EDocumentStatus.Verified;

            await eventPublisher.PublishAsync(
                new KycApproved(customer.Id, customer.CifNumber, DateTimeOffset.UtcNow),
                cancellationToken);
        }
        else
        {
            customer.KycStatus = EKycStatus.Rejected;
            customer.KycLevel = EKycLevel.Basic;
            customer.KycReviewedAt = DateTimeOffset.UtcNow;
            customer.KycExpiresAt = null;
            customer.KycRejectReason = request.RejectReason?.Trim();

            foreach (var document in customer.Documents.Where(x => x.Status == EDocumentStatus.Uploaded))
            {
                document.Status = EDocumentStatus.Rejected;
                document.RejectReason = customer.KycRejectReason;
            }
        }

        unitOfWork.CustomerRepository.Update(customer);
        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            CustomerId = customer.Id,
            Action = request.Approved ? EAuditAction.KycApproved : EAuditAction.KycRejected,
            Resource = "Kyc",
            IpAddress = request.IpAddress
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new ReviewKycResponse
        {
            Message = request.Approved ? "KYC onaylandı. Müşteri aktif." : "KYC reddedildi.",
            Customer = CustomerMapper.ToDto(customer)
        };
    }
}
