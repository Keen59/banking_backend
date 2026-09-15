using Banking.Contracts.Events;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.ExecuteInternalTransfer;

public class ExecuteInternalTransferHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<ExecuteInternalTransferCommand, ExecuteInternalTransferResponse>
{
    public async Task<ExecuteInternalTransferResponse> Handle(
        ExecuteInternalTransferCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        if (request.SourceAccountId == request.DestinationAccountId)
            throw new InvalidOperationException("Kaynak ve hedef hesap aynı olamaz.");

        var journalKey = JournalKey(request.TransferId);
        var holdKey = HoldKey(request.TransferId);

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
        {
            await eventPublisher.PublishAsync(
                new TransferCompleted(request.TransferId, existingJournal.Id, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new ExecuteInternalTransferResponse
            {
                Message = "Virman zaten kayıtlı.",
                JournalEntryId = existingJournal.Id
            };
        }

        var source = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(request.SourceAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Kaynak ledger hesabı bulunamadı.");
        var destination = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(request.DestinationAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Hedef ledger hesabı bulunamadı.");

        if (source.Kind != ELedgerAccountKind.CustomerDemandDeposit ||
            destination.Kind != ELedgerAccountKind.CustomerDemandDeposit)
            throw new InvalidOperationException("Virman yalnızca vadesiz müşteri hesapları arasında yapılır.");

        if (source.Status != ELedgerAccountStatus.Active || destination.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba virman yapılamaz.");

        if (!string.Equals(source.Currency, request.Currency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(destination.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Para birimi uyuşmuyor.");

        var amount = decimal.Round(request.Amount, 4, MidpointRounding.AwayFromZero);
        var sourceLines = await unitOfWork.JournalEntries.GetLinesByLedgerAccountIdAsync(source.Id, cancellationToken);
        var sourceHolds = await unitOfWork.Holds.GetActiveByLedgerAccountIdAsync(source.Id, cancellationToken);
        var (_, _, available) = LedgerBalanceCalculator.Calculate(source.Kind, sourceLines, sourceHolds);
        if (amount > available)
            throw new InvalidOperationException("Available yetersiz.");

        var hold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken);
        if (hold is null)
        {
            hold = new AccountHold
            {
                Id = Guid.NewGuid(),
                LedgerAccountId = source.Id,
                IdempotencyKey = holdKey,
                Amount = amount,
                Status = EHoldStatus.Active
            };
            await unitOfWork.Holds.AddAsync(hold, cancellationToken);
        }

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = journalKey,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? "İç virman"
                : request.Description.Trim(),
            BookedAt = DateTimeOffset.UtcNow,
            Lines =
            [
                new JournalLine
                {
                    Id = Guid.NewGuid(),
                    LedgerAccountId = source.Id,
                    Side = EEntrySide.Debit,
                    Amount = amount
                },
                new JournalLine
                {
                    Id = Guid.NewGuid(),
                    LedgerAccountId = destination.Id,
                    Side = EEntrySide.Credit,
                    Amount = amount
                }
            ]
        };

        await unitOfWork.JournalEntries.AddAsync(entry, cancellationToken);

        hold.Status = EHoldStatus.Released;
        unitOfWork.Holds.Update(hold);

        await eventPublisher.PublishAsync(
            new TransferCompleted(request.TransferId, entry.Id, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ExecuteInternalTransferResponse
        {
            Message = "Virman posting kaydedildi.",
            JournalEntryId = entry.Id
        };
    }

    public static string JournalKey(Guid transferId) => $"transfer:{transferId:D}";

    public static string HoldKey(Guid transferId) => $"transfer-hold:{transferId:D}";
}
