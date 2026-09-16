using Banking.Contracts.Events;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.ApplyTestCredit;

public class ApplyTestCreditHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<ApplyTestCreditCommand, ApplyTestCreditResponse>
{
    public async Task<ApplyTestCreditResponse> Handle(
        ApplyTestCreditCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        var journalKey = JournalKey(request.CreditId);
        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
        {
            await eventPublisher.PublishAsync(
                new TestCreditPosted(request.CreditId, existingJournal.Id, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new ApplyTestCreditResponse
            {
                Message = "Test kredisi zaten kayıtlı.",
                JournalEntryId = existingJournal.Id
            };
        }

        var customerAccount = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
                request.AccountId,
                cancellationToken)
            ?? throw new InvalidOperationException("Hedef ledger hesabı bulunamadı.");

        if (customerAccount.Kind != ELedgerAccountKind.CustomerDemandDeposit)
            throw new InvalidOperationException("Test kredisi yalnızca vadesiz müşteri hesabına yazılır.");

        if (customerAccount.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        if (!string.Equals(customerAccount.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Para birimi uyuşmuyor.");

        var clearing = await unitOfWork.LedgerAccounts.GetByIdAsync(SystemLedgerAccounts.InternalClearingTry)
            ?? throw new InvalidOperationException("Clearing hesabı bulunamadı.");

        if (clearing.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        if (!string.Equals(clearing.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Para birimi uyuşmuyor.");

        var amount = decimal.Round(request.Amount, 4, MidpointRounding.AwayFromZero);
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = journalKey,
            Description = "Test kredisi",
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
                    LedgerAccountId = customerAccount.Id,
                    Side = EEntrySide.Credit,
                    Amount = amount
                }
            ]
        };

        await unitOfWork.JournalEntries.AddAsync(entry, cancellationToken);
        await eventPublisher.PublishAsync(
            new TestCreditPosted(request.CreditId, entry.Id, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ApplyTestCreditResponse
        {
            Message = "Test kredisi kaydedildi.",
            JournalEntryId = entry.Id
        };
    }

    public static string JournalKey(Guid creditId) => $"test-credit:{creditId:D}";
}
