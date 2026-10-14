using MediatR;
using OpenBankingService.Application.DTOs;
using OpenBankingService.Application.DTOs.Accounts;
using OpenBankingService.Application.Helpers;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Application.Mapping;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.Commands.GetAisAccount;

public class GetAisAccountCommand : IRequest<GetAisAccountResponse>
{
    public string AccessToken { get; set; } = string.Empty;

    public Guid AccountId { get; set; }
}

public class GetAisAccountResponse : Response
{
    public AisAccountDto? Data { get; set; }
}

public class GetAisAccountHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAisAccountCommand, GetAisAccountResponse>
{
    public async Task<GetAisAccountResponse> Handle(
        GetAisAccountCommand request,
        CancellationToken cancellationToken)
    {
        var consent = ConsentAccess.RequireActive(
            await unitOfWork.Consents.GetByAccessTokenAsync(request.AccessToken, cancellationToken),
            EConsentScope.Ais,
            DateTimeOffset.UtcNow);

        if (consent.Accounts.All(x => x.AccountId != request.AccountId))
            throw new KeyNotFoundException("Account not found.");

        var projection = await unitOfWork.AccountProjections.GetByIdAsync(request.AccountId)
            ?? throw new KeyNotFoundException("Account not found.");

        return new GetAisAccountResponse
        {
            Message = "Account retrieved.",
            Data = OpenBankingMapper.ToAisDto(projection)
        };
    }
}
