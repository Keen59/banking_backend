using Banking.Contracts.Events;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.HoldCardAuthorization;

public class HoldCardAuthorizationHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<HoldCardAuthorizationCommand, HoldCardAuthorizationResponse>
{
    public async Task<HoldCardAuthorizationResponse> Handle(
        HoldCardAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        var journalKey = JournalKey(request.AuthorizationId);
        var holdKey = HoldKey(request.AuthorizationId);

        var existingJournal = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(journalKey, cancellationToken);
        if (existingJournal is not null)
        {
            await eventPublisher.PublishAsync(
                new CardAuthorizationCompleted(request.AuthorizationId, existingJournal.Id, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new HoldCardAuthorizationResponse
            {
                Message = "Kart yetkisi zaten tahsil edilmiş.",
                JournalEntryId = existingJournal.Id
            };
        }

        var existingHold = await unitOfWork.Holds.GetByIdempotencyKeyAsync(holdKey, cancellationToken);
        if (existingHold is not null)
        {
            if (existingHold.Status == EHoldStatus.Released)
            {
                await eventPublisher.PublishAsync(
                    new CardAuthorizationRejected(request.AuthorizationId, "Kart yetkisi iptal edilmiş.", DateTimeOffset.UtcNow),
                    cancellationToken);
                await unitOfWork.SaveAsync(cancellationToken);
                return new HoldCardAuthorizationResponse { Message = "Kart yetkisi iptal edilmiş." };
            }

            await eventPublisher.PublishAsync(
                new CardAuthorizationHeld(request.AuthorizationId, DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new HoldCardAuthorizationResponse { Message = "Kart hold zaten kayıtlı." };
        }

        var source = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
                request.AccountId,
                cancellationToken)
            ?? throw new InvalidOperationException("Kaynak ledger hesabı bulunamadı.");

        if (source.Kind != ELedgerAccountKind.CustomerDemandDeposit)
            throw new InvalidOperationException("Kart yetkisi yalnızca vadesiz müşteri hesabından yapılır.");

        if (source.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba kart işlemi yapılamaz.");

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
            new CardAuthorizationHeld(request.AuthorizationId, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new HoldCardAuthorizationResponse { Message = "Kart hold yazıldı." };
    }

    public static string JournalKey(Guid authorizationId) => $"card-auth:{authorizationId:D}";

    public static string HoldKey(Guid authorizationId) => $"card-auth-hold:{authorizationId:D}";
}
