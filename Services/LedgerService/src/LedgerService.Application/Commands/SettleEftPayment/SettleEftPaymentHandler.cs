using Banking.Contracts.Events;
using LedgerService.Application.Commands.HoldEftPayment;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.SettleEftPayment;

public class SettleEftPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<SettleEftPaymentCommand, SettleEftPaymentResponse>
{
    public async Task<SettleEftPaymentResponse> Handle(
        SettleEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var journalKey = HoldEftPaymentHandler.JournalKey(request.EftPaymentId);
        var holdKey = HoldEftPaymentHandler.HoldKey(request.EftPaymentId);

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
        {
            await eventPublisher.PublishAsync(
                new EftPaymentCompleted(request.EftPaymentId, existingJournal.Id, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new SettleEftPaymentResponse
            {
                Message = "EFT zaten takas edildi.",
                JournalEntryId = existingJournal.Id
            };
        }

        var hold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken)
            ?? throw new InvalidOperationException("EFT hold bulunamadı.");

        if (hold.Status != EHoldStatus.Active)
            throw new InvalidOperationException("EFT iade edilmiş.");

        var source = await unitOfWork.LedgerAccounts.GetByIdAsync(hold.LedgerAccountId)
            ?? throw new InvalidOperationException("Kaynak ledger hesabı bulunamadı.");

        var clearing = await unitOfWork.LedgerAccounts.GetByIdAsync(SystemLedgerAccounts.InternalClearingTry)
            ?? throw new InvalidOperationException("Clearing hesabı bulunamadı.");

        if (clearing.Status != ELedgerAccountStatus.Active || source.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        var amount = hold.Amount;
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = journalKey,
            Description = string.IsNullOrWhiteSpace(request.Description) ? "EFT takas" : request.Description.Trim(),
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
                    LedgerAccountId = clearing.Id,
                    Side = EEntrySide.Credit,
                    Amount = amount
                }
            ]
        };

        await unitOfWork.JournalEntries.AddAsync(entry, cancellationToken);
        hold.Status = EHoldStatus.Released;
        unitOfWork.Holds.Update(hold);

        await eventPublisher.PublishAsync(
            new EftPaymentCompleted(request.EftPaymentId, entry.Id, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new SettleEftPaymentResponse
        {
            Message = "EFT takas kaydedildi.",
            JournalEntryId = entry.Id
        };
    }
}
