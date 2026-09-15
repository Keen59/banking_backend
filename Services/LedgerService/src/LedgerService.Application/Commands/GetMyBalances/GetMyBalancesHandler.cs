using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using MediatR;

namespace LedgerService.Application.Commands.GetMyBalances;

public class GetMyBalancesHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyBalancesCommand, GetMyBalancesResponse>
{
    public async Task<GetMyBalancesResponse> Handle(GetMyBalancesCommand request, CancellationToken cancellationToken)
    {
        var accounts = await unitOfWork.LedgerAccounts.GetByCustomerIdAsync(request.CustomerId, cancellationToken);
        var balances = new List<BalanceDto>();

        foreach (var account in accounts)
        {
            var lines = await unitOfWork.JournalEntries.GetLinesByLedgerAccountIdAsync(account.Id, cancellationToken);
            var holds = await unitOfWork.Holds.GetActiveByLedgerAccountIdAsync(account.Id, cancellationToken);
            var (ledger, hold, available) = LedgerBalanceCalculator.Calculate(account.Kind, lines, holds);
            balances.Add(new BalanceDto
            {
                AccountId = account.SourceAccountId ?? Guid.Empty,
                LedgerAccountId = account.Id,
                CustomerId = account.CustomerId ?? Guid.Empty,
                Currency = account.Currency,
                Ledger = ledger,
                Hold = hold,
                Available = available
            });
        }

        return new GetMyBalancesResponse
        {
            Message = "Bakiye listesi.",
            Balances = balances
        };
    }
}
