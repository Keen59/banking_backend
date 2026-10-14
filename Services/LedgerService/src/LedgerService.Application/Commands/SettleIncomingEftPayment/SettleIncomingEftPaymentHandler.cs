using Banking.Contracts.Events;
using LedgerService.Application.Commands.AcceptIncomingEftPayment;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.SettleIncomingEftPayment;

public class SettleIncomingEftPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<SettleIncomingEftPaymentCommand, SettleIncomingEftPaymentResponse>
{
    public async Task<SettleIncomingEftPaymentResponse> Handle(
        SettleIncomingEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var journalKey = AcceptIncomingEftPaymentHandler.JournalKey(request.IncomingEftPaymentId);
        var holdKey = AcceptIncomingEftPaymentHandler.HoldKey(request.IncomingEftPaymentId);

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
        {
            await eventPublisher.PublishAsync(
                new IncomingEftPaymentCompleted(
                    request.IncomingEftPaymentId,
                    existingJournal.Id,
                    DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new SettleIncomingEftPaymentResponse
            {
                Message = "Gelen EFT zaten takas edildi.",
                JournalEntryId = existingJournal.Id
            };
        }

        var hold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken)
            ?? throw new InvalidOperationException("Gelen EFT kabul kaydı bulunamadı.");

        if (hold.Status != EHoldStatus.Active)
            throw new InvalidOperationException("Gelen EFT iade edilmiş.");

        var destination = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
                request.DestinationAccountId,
                cancellationToken)
            ?? throw new InvalidOperationException("Hedef ledger hesabı bulunamadı.");

        var clearing = await unitOfWork.LedgerAccounts.GetByIdAsync(SystemLedgerAccounts.InternalClearingTry)
            ?? throw new InvalidOperationException("Clearing hesabı bulunamadı.");

        if (clearing.Status != ELedgerAccountStatus.Active || destination.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        var amount = hold.Amount;
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = journalKey,
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Gelen EFT takas" : request.Description.Trim(),
            BookedAt = DateTimeOffset.UtcNow,
            Lines =
            [
                new JournalLine
                {
                    Id = Guid.NewGuid(),
                    LedgerAccountId = clearing.Id,
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
            new IncomingEftPaymentCompleted(
                request.IncomingEftPaymentId,
                entry.Id,
                DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new SettleIncomingEftPaymentResponse
        {
            Message = "Gelen EFT takas kaydedildi.",
            JournalEntryId = entry.Id
        };
    }
}
