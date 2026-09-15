using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.PlaceHold;

public class PlaceHoldHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<PlaceHoldCommand, PlaceHoldResponse>
{
    public async Task<PlaceHoldResponse> Handle(PlaceHoldCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new InvalidOperationException("IdempotencyKey zorunludur.");

        if (request.Amount <= 0)
            throw new InvalidOperationException("Hold tutarı sıfırdan büyük olmalıdır.");

        var existing = await unitOfWork.Holds.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var account = await unitOfWork.LedgerAccounts.GetByIdAsync(existing.LedgerAccountId)
                ?? throw new InvalidOperationException("Ledger hesabı bulunamadı.");
            return new PlaceHoldResponse
            {
                Message = "Hold zaten kayıtlı.",
                HoldId = existing.Id,
                Balance = await LoadBalance(account, cancellationToken)
            };
        }

        var ledgerAccount = await unitOfWork.LedgerAccounts.GetByIdAsync(request.LedgerAccountId)
            ?? throw new InvalidOperationException("Ledger hesabı bulunamadı.");

        if (ledgerAccount.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba hold yazılamaz.");

        var balance = await LoadBalance(ledgerAccount, cancellationToken);
        if (request.Amount > balance.Available)
            throw new InvalidOperationException("Available yetersiz.");

        var hold = new AccountHold
        {
            Id = Guid.NewGuid(),
            LedgerAccountId = ledgerAccount.Id,
            IdempotencyKey = request.IdempotencyKey.Trim(),
            Amount = decimal.Round(request.Amount, 4, MidpointRounding.AwayFromZero),
            Status = EHoldStatus.Active
        };

        await unitOfWork.Holds.AddAsync(hold, cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new PlaceHoldResponse
        {
            Message = "Hold yazıldı.",
            HoldId = hold.Id,
            Balance = await LoadBalance(ledgerAccount, cancellationToken)
        };
    }

    private async Task<BalanceDto> LoadBalance(LedgerAccount account, CancellationToken cancellationToken)
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
