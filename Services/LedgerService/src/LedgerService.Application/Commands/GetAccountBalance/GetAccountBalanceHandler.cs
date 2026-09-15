using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using MediatR;

namespace LedgerService.Application.Commands.GetAccountBalance;

public class GetAccountBalanceHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAccountBalanceCommand, GetAccountBalanceResponse>
{
    public async Task<GetAccountBalanceResponse> Handle(
        GetAccountBalanceCommand request,
        CancellationToken cancellationToken)
    {
        var account = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
            request.AccountId,
            cancellationToken) ?? throw new InvalidOperationException("Ledger hesabı bulunamadı.");

        if (!request.CanReadAny && account.CustomerId != request.RequestedCustomerId)
            throw new UnauthorizedAccessException("Bu hesaba erişim yetkiniz yok.");

        var lines = await unitOfWork.JournalEntries.GetLinesByLedgerAccountIdAsync(account.Id, cancellationToken);
        var holds = await unitOfWork.Holds.GetActiveByLedgerAccountIdAsync(account.Id, cancellationToken);
        var (ledger, hold, available) = LedgerBalanceCalculator.Calculate(account.Kind, lines, holds);

        return new GetAccountBalanceResponse
        {
            Message = "Bakiye kaydı.",
            Balance = new BalanceDto
            {
                AccountId = account.SourceAccountId ?? Guid.Empty,
                LedgerAccountId = account.Id,
                CustomerId = account.CustomerId ?? Guid.Empty,
                Currency = account.Currency,
                Ledger = ledger,
                Hold = hold,
                Available = available
            }
        };
    }
}
