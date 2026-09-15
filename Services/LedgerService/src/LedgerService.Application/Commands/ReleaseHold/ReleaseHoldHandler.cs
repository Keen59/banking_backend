using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.ReleaseHold;

public class ReleaseHoldHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ReleaseHoldCommand, ReleaseHoldResponse>
{
    public async Task<ReleaseHoldResponse> Handle(ReleaseHoldCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new InvalidOperationException("IdempotencyKey zorunludur.");

        var hold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken)
            ?? throw new InvalidOperationException("Hold bulunamadı.");

        if (hold.Status == EHoldStatus.Released)
        {
            var already = await unitOfWork.LedgerAccounts.GetByIdAsync(hold.LedgerAccountId)
                ?? throw new InvalidOperationException("Ledger hesabı bulunamadı.");
            return new ReleaseHoldResponse
            {
                Message = "Hold zaten çözüldü.",
                Balance = await LoadBalance(already, cancellationToken)
            };
        }

        hold.Status = EHoldStatus.Released;
        unitOfWork.Holds.Update(hold);
        await unitOfWork.SaveAsync(cancellationToken);

        var account = await unitOfWork.LedgerAccounts.GetByIdAsync(hold.LedgerAccountId)
            ?? throw new InvalidOperationException("Ledger hesabı bulunamadı.");

        return new ReleaseHoldResponse
        {
            Message = "Hold çözüldü.",
            Balance = await LoadBalance(account, cancellationToken)
        };
    }

    private async Task<BalanceDto> LoadBalance(Domain.Entities.LedgerAccount account, CancellationToken cancellationToken)
    {
        var lines = await unitOfWork.JournalEntries.GetLinesByLedgerAccountIdAsync(account.Id, cancellationToken);
        var holds = await unitOfWork.Holds.GetActiveByLedgerAccountIdAsync(account.Id, cancellationToken);
        var (ledger, hold, available) = LedgerBalanceCalculator.Calculate(account.Kind, lines, holds);
        return new BalanceDto
        {
            AccountId = account.SourceAccountId ?? Guid.Empty,
            LedgerAccountId = account.Id,
            CustomerId = account.CustomerId ?? Guid.Empty,
            Currency = account.Currency,
            Ledger = ledger,
            Hold = hold,
            Available = available
        };
    }
}
