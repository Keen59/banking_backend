using MediatR;
using OpenBankingService.Application.DTOs;
using OpenBankingService.Application.DTOs.Payments;
using OpenBankingService.Application.Helpers;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Application.Mapping;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.Commands.GetPaymentInitiation;

public class GetPaymentInitiationCommand : IRequest<GetPaymentInitiationResponse>
{
    public string AccessToken { get; set; } = string.Empty;

    public Guid InitiationId { get; set; }
}

public class GetPaymentInitiationResponse : Response
{
    public PaymentInitiationDto? Data { get; set; }
}

public class GetPaymentInitiationHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPaymentInitiationCommand, GetPaymentInitiationResponse>
{
    public async Task<GetPaymentInitiationResponse> Handle(
        GetPaymentInitiationCommand request,
        CancellationToken cancellationToken)
    {
        var consent = ConsentAccess.RequireActive(
            await unitOfWork.Consents.GetByAccessTokenAsync(request.AccessToken, cancellationToken),
            EConsentScope.Pis,
            DateTimeOffset.UtcNow);

        var initiation = await unitOfWork.PaymentInitiations.GetByIdAsync(request.InitiationId);
        if (initiation is null || initiation.ConsentId != consent.Id)
            throw new KeyNotFoundException("Payment initiation not found.");

        return new GetPaymentInitiationResponse
        {
            Message = "Payment initiation retrieved.",
            Data = OpenBankingMapper.ToDto(initiation)
        };
    }
}
