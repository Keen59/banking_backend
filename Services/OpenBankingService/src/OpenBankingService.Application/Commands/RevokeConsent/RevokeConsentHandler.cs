using MediatR;
using OpenBankingService.Application.DTOs;
using OpenBankingService.Application.DTOs.Consents;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Application.Mapping;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.Commands.RevokeConsent;

public class RevokeConsentCommand : IRequest<RevokeConsentResponse>
{
    public Guid ConsentId { get; set; }

    public Guid CustomerId { get; set; }
}

public class RevokeConsentResponse : Response
{
    public ConsentDto? Data { get; set; }
}

public class RevokeConsentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeConsentCommand, RevokeConsentResponse>
{
    public async Task<RevokeConsentResponse> Handle(
        RevokeConsentCommand request,
        CancellationToken cancellationToken)
    {
        var consent = await unitOfWork.Consents.GetByIdWithAccountsAsync(request.ConsentId, cancellationToken);
        if (consent is null || consent.CustomerId != request.CustomerId)
            throw new KeyNotFoundException("Consent not found.");

        if (consent.Status != EConsentStatus.Active)
            throw new InvalidOperationException("Consent is not active.");

        consent.Status = EConsentStatus.Revoked;
        consent.UpdatedAt = DateTimeOffset.UtcNow;
        unitOfWork.Consents.Update(consent);
        await unitOfWork.SaveAsync(cancellationToken);

        return new RevokeConsentResponse
        {
            Message = "Consent revoked.",
            Data = OpenBankingMapper.ToDto(consent)
        };
    }
}
