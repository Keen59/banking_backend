using Banking.Contracts.Events;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.AcceptIncomingEftPayment;

public class AcceptIncomingEftPaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<AcceptIncomingEftPaymentCommand, AcceptIncomingEftPaymentResponse>
{
    public async Task<AcceptIncomingEftPaymentResponse> Handle(
        AcceptIncomingEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        var journalKey = JournalKey(request.IncomingEftPaymentId);
        var holdKey = HoldKey(request.IncomingEftPaymentId);

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
            return new AcceptIncomingEftPaymentResponse
            {
                Message = "Gelen EFT zaten takas edildi.",
                JournalEntryId = existingJournal.Id
            };
        }

        var existingHold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken);
        if (existingHold is not null)
        {
            if (existingHold.Status == EHoldStatus.Released)
            {
                await eventPublisher.PublishAsync(
                    new IncomingEftPaymentRejected(
                        request.IncomingEftPaymentId,
                        "Gelen EFT iade edilmiş.",
                        DateTimeOffset.UtcNow),
                    cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);
                return new AcceptIncomingEftPaymentResponse { Message = "Gelen EFT iade edilmiş." };
            }

            await eventPublisher.PublishAsync(
                new IncomingEftPaymentHeld(request.IncomingEftPaymentId, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new AcceptIncomingEftPaymentResponse { Message = "Gelen EFT zaten kabul edildi." };
        }

        var destination = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
                request.DestinationAccountId,
                cancellationToken)
            ?? throw new InvalidOperationException("Hedef ledger hesabı bulunamadı.");

        if (destination.Kind != ELedgerAccountKind.CustomerDemandDeposit)
            throw new InvalidOperationException("Gelen EFT yalnızca vadesiz müşteri hesabına yazılır.");

        if (destination.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba gelen EFT yazılamaz.");

        if (!string.Equals(destination.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Para birimi uyuşmuyor.");

        var clearing = await unitOfWork.LedgerAccounts.GetByIdAsync(SystemLedgerAccounts.InternalClearingTry)
            ?? throw new InvalidOperationException("Clearing hesabı bulunamadı.");

        if (clearing.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        var amount = decimal.Round(request.Amount, 4, MidpointRounding.AwayFromZero);
        await unitOfWork.Holds.AddAsync(new AccountHold
        {
            Id = Guid.NewGuid(),
            LedgerAccountId = clearing.Id,
            IdempotencyKey = holdKey,
            Amount = amount,
            Status = EHoldStatus.Active
        }, cancellationToken);

        await eventPublisher.PublishAsync(
            new IncomingEftPaymentHeld(request.IncomingEftPaymentId, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new AcceptIncomingEftPaymentResponse { Message = "Gelen EFT kabul edildi." };
    }

    public static string JournalKey(Guid incomingEftPaymentId) => $"incoming-eft:{incomingEftPaymentId:D}";

    public static string HoldKey(Guid incomingEftPaymentId) => $"incoming-eft-hold:{incomingEftPaymentId:D}";
}
