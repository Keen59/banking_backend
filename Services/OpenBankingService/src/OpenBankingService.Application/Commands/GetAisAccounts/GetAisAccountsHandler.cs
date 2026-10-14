using MediatR;
using OpenBankingService.Application.DTOs;
using OpenBankingService.Application.DTOs.Accounts;
using OpenBankingService.Application.Helpers;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Application.Mapping;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.Commands.GetAisAccounts;

public class GetAisAccountsCommand : IRequest<GetAisAccountsResponse>
{
    public string AccessToken { get; set; } = string.Empty;
}

public class GetAisAccountsResponse : Response
{
    public IReadOnlyList<AisAccountDto> Data { get; set; } = [];
}

public class GetAisAccountsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAisAccountsCommand, GetAisAccountsResponse>
{
    public async Task<GetAisAccountsResponse> Handle(
        GetAisAccountsCommand request,
        CancellationToken cancellationToken)
    {
        var consent = ConsentAccess.RequireActive(
            await unitOfWork.Consents.GetByAccessTokenAsync(request.AccessToken, cancellationToken),
            EConsentScope.Ais,
            DateTimeOffset.UtcNow);

        var accounts = new List<AisAccountDto>();
        foreach (var link in consent.Accounts)
        {
            var projection = await unitOfWork.AccountProjections.GetByIdAsync(link.AccountId);
            if (projection is null)
                continue;

            accounts.Add(OpenBankingMapper.ToAisDto(projection));
        }

        return new GetAisAccountsResponse
        {
            Message = "Accounts listed.",
            Data = accounts
        };
    }
}
