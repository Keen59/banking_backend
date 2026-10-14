using Banking.Contracts.Events;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.ApplyLoanRepayment;

public class ApplyLoanRepaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<ApplyLoanRepaymentCommand, ApplyLoanRepaymentResponse>
{
    public async Task<ApplyLoanRepaymentResponse> Handle(
        ApplyLoanRepaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        var journalKey = JournalKey(request.InstallmentId);
        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
        {
            await eventPublisher.PublishAsync(
                new LoanRepaymentPosted(request.LoanId, request.InstallmentId, existingJournal.Id, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new ApplyLoanRepaymentResponse
            {
                Message = "Taksit ödemesi zaten kayıtlı.",
                JournalEntryId = existingJournal.Id
            };
        }

        var source = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
                request.AccountId,
                cancellationToken)
            ?? throw new InvalidOperationException("Kaynak ledger hesabı bulunamadı.");

        if (source.Kind != ELedgerAccountKind.CustomerDemandDeposit)
            throw new InvalidOperationException("Taksit yalnızca vadesiz müşteri hesabından ödenir.");

        if (source.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        if (!string.Equals(source.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Para birimi uyuşmuyor.");

        var clearing = await unitOfWork.LedgerAccounts.GetByIdAsync(SystemLedgerAccounts.InternalClearingTry)
            ?? throw new InvalidOperationException("Clearing hesabı bulunamadı.");

        if (clearing.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        var amount = decimal.Round(request.Amount, 4, MidpointRounding.AwayFromZero);
        var sourceLines = await unitOfWork.JournalEntries.GetLinesByLedgerAccountIdAsync(source.Id, cancellationToken);
        var sourceHolds = await unitOfWork.Holds.GetActiveByLedgerAccountIdAsync(source.Id, cancellationToken);
        var (_, _, available) = LedgerBalanceCalculator.Calculate(source.Kind, sourceLines, sourceHolds);
        if (amount > available)
            throw new InvalidOperationException("Available yetersiz.");

        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = journalKey,
            Description = "Kredi taksit",
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
        await eventPublisher.PublishAsync(
            new LoanRepaymentPosted(request.LoanId, request.InstallmentId, entry.Id, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ApplyLoanRepaymentResponse
        {
            Message = "Taksit ödemesi kaydedildi.",
            JournalEntryId = entry.Id
        };
    }

    public static string JournalKey(Guid installmentId) => $"loan-repay:{installmentId:D}";
}
