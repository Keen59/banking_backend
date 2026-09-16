using Banking.Contracts.Events;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.ExecuteFastPayment;

public class ExecuteFastPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<ExecuteFastPaymentCommand, ExecuteFastPaymentResponse>
{
    public async Task<ExecuteFastPaymentResponse> Handle(
        ExecuteFastPaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        var journalKey = JournalKey(request.FastPaymentId);
        var holdKey = HoldKey(request.FastPaymentId);

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
        {
            await eventPublisher.PublishAsync(
                new FastPaymentCompleted(request.FastPaymentId, existingJournal.Id, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new ExecuteFastPaymentResponse
            {
                Message = "FAST zaten kayıtlı.",
                JournalEntryId = existingJournal.Id
            };
        }

        var source = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
                request.SourceAccountId,
                cancellationToken)
            ?? throw new InvalidOperationException("Kaynak ledger hesabı bulunamadı.");

        if (source.Kind != ELedgerAccountKind.CustomerDemandDeposit)
            throw new InvalidOperationException("FAST yalnızca vadesiz müşteri hesabından yapılır.");

        if (source.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba virman yapılamaz.");

        if (!string.Equals(source.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Para birimi uyuşmuyor.");

        var clearing = await unitOfWork.LedgerAccounts.GetByIdAsync(SystemLedgerAccounts.InternalClearingTry)
            ?? throw new InvalidOperationException("Clearing hesabı bulunamadı.");

        if (clearing.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        if (!string.Equals(clearing.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
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

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"FAST {request.DestinationIban}"
            : request.Description.Trim();

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = journalKey,
            Description = description,
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
            new FastPaymentCompleted(request.FastPaymentId, entry.Id, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ExecuteFastPaymentResponse
        {
            Message = "FAST posting kaydedildi.",
            JournalEntryId = entry.Id
        };
    }

    public static string JournalKey(Guid fastPaymentId) => $"fast:{fastPaymentId:D}";

    public static string HoldKey(Guid fastPaymentId) => $"fast-hold:{fastPaymentId:D}";
}
