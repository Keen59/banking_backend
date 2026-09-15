using AccountService.Application.Interfaces.Repositories;
using AccountService.Application.Mapping;
using MediatR;

namespace AccountService.Application.Commands.GetAccount;

public class GetAccountHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAccountCommand, GetAccountResponse>
{
    public async Task<GetAccountResponse> Handle(GetAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await unitOfWork.AccountRepository.GetByIdAsync(request.AccountId)
            ?? throw new InvalidOperationException("Hesap bulunamadı.");

        if (!request.CanReadAny && account.CustomerId != request.RequestedCustomerId)
            throw new UnauthorizedAccessException("Bu hesaba erişim yetkiniz yok.");

        return new GetAccountResponse
        {
            Message = "Hesap kaydı.",
            Account = AccountMapper.ToDto(account)
        };
    }
}
