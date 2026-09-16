using Banking.Contracts.Events;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.HoldEftPayment;

public class HoldEftPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<HoldEftPaymentCommand, HoldEftPaymentResponse>
{
    public async Task<HoldEftPaymentResponse> Handle(
        HoldEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        var journalKey = JournalKey(request.EftPaymentId);
        var holdKey = HoldKey(request.EftPaymentId);

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
        {
            await eventPublisher.PublishAsync(
                new EftPaymentCompleted(request.EftPaymentId, existingJournal.Id, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new HoldEftPaymentResponse
            {
                Message = "EFT zaten takas edildi.",
                JournalEntryId = existingJournal.Id
            };
        }

        var existingHold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken);
        if (existingHold is not null)
        {
            if (existingHold.Status == EHoldStatus.Released)
            {
                await eventPublisher.PublishAsync(
                    new EftPaymentRejected(request.EftPaymentId, "EFT iade edilmiş.", DateTimeOffset.UtcNow),
                    cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);
                return new HoldEftPaymentResponse { Message = "EFT iade edilmiş." };
            }

            await eventPublisher.PublishAsync(
                new EftPaymentHeld(request.EftPaymentId, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new HoldEftPaymentResponse { Message = "EFT hold zaten kayıtlı." };
        }

        var source = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
                request.SourceAccountId,
                cancellationToken)
            ?? throw new InvalidOperationException("Kaynak ledger hesabı bulunamadı.");

        if (source.Kind != ELedgerAccountKind.CustomerDemandDeposit)
            throw new InvalidOperationException("EFT yalnızca vadesiz müşteri hesabından yapılır.");

        if (source.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba EFT yapılamaz.");

        if (!string.Equals(source.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Para birimi uyuşmuyor.");

        var amount = decimal.Round(request.Amount, 4, MidpointRounding.AwayFromZero);
        var sourceLines = await unitOfWork.JournalEntries.GetLinesByLedgerAccountIdAsync(source.Id, cancellationToken);
        var sourceHolds = await unitOfWork.Holds.GetActiveByLedgerAccountIdAsync(source.Id, cancellationToken);
        var (_, _, available) = LedgerBalanceCalculator.Calculate(source.Kind, sourceLines, sourceHolds);
        if (amount > available)
            throw new InvalidOperationException("Available yetersiz.");

        await unitOfWork.Holds.AddAsync(new AccountHold
        {
            Id = Guid.NewGuid(),
            LedgerAccountId = source.Id,
            IdempotencyKey = holdKey,
            Amount = amount,
            Status = EHoldStatus.Active
        }, cancellationToken);

        await eventPublisher.PublishAsync(
            new EftPaymentHeld(request.EftPaymentId, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new HoldEftPaymentResponse { Message = "EFT hold yazıldı." };
    }

    public static string JournalKey(Guid eftPaymentId) => $"eft:{eftPaymentId:D}";

    public static string HoldKey(Guid eftPaymentId) => $"eft-hold:{eftPaymentId:D}";
}
