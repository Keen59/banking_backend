using Banking.Contracts.Events;
using LedgerService.Application.Commands.HoldCardAuthorization;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.CaptureCardAuthorization;

public class CaptureCardAuthorizationHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<CaptureCardAuthorizationCommand, CaptureCardAuthorizationResponse>
{
    public async Task<CaptureCardAuthorizationResponse> Handle(
        CaptureCardAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        var journalKey = HoldCardAuthorizationHandler.JournalKey(request.AuthorizationId);
        var holdKey = HoldCardAuthorizationHandler.HoldKey(request.AuthorizationId);

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
        {
            await eventPublisher.PublishAsync(
                new CardAuthorizationCompleted(request.AuthorizationId, existingJournal.Id, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new CaptureCardAuthorizationResponse
            {
                Message = "Kart yetkisi zaten tahsil edilmiş.",
                JournalEntryId = existingJournal.Id
            };
        }

        var hold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken)
            ?? throw new InvalidOperationException("Kart hold bulunamadı.");

        if (hold.Status != EHoldStatus.Active)
            throw new InvalidOperationException("Kart yetkisi iptal edilmiş.");

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
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Kart tahsil" : request.Description.Trim(),
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
            new CardAuthorizationCompleted(request.AuthorizationId, entry.Id, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new CaptureCardAuthorizationResponse
        {
            Message = "Kart tahsil kaydedildi.",
            JournalEntryId = entry.Id
        };
    }
}
