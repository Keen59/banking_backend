using Banking.Contracts.Events;
using LedgerService.Application.Helpers;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MediatR;

namespace LedgerService.Application.Commands.ApplyFxConversion;

public class ApplyFxConversionHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
    : IRequestHandler<ApplyFxConversionCommand, ApplyFxConversionResponse>
{
    public async Task<ApplyFxConversionResponse> Handle(
        ApplyFxConversionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.SourceAmount <= 0 || request.DestinationAmount <= 0)
            throw new InvalidOperationException("Tutar sıfırdan büyük olmalıdır.");

        if (request.SourceAccountId == request.DestinationAccountId)
            throw new InvalidOperationException("Kaynak ve hedef hesap aynı olamaz.");

        if (string.Equals(request.SourceCurrency, request.DestinationCurrency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Aynı para birimi için virman kullanın.");

        var sellKey = SellJournalKey(request.ConversionId);
        var buyKey = BuyJournalKey(request.ConversionId);

        var existingSell = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(sellKey, cancellationToken);
        var existingBuy = await unitOfWork.JournalEntries.GetByIdempotencyKeyAsync(buyKey, cancellationToken);
        if (existingSell is not null && existingBuy is not null)
        {
            await eventPublisher.PublishAsync(
                new FxConversionCompleted(
                    request.ConversionId,
                    existingSell.Id,
                    existingBuy.Id,
                    DateTimeOffset.UtcNow),
                cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);
            return new ApplyFxConversionResponse
            {
                Message = "Döviz çevrimi zaten kayıtlı.",
                SellJournalEntryId = existingSell.Id,
                BuyJournalEntryId = existingBuy.Id
            };
        }

        var source = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
                request.SourceAccountId,
                cancellationToken)
            ?? throw new InvalidOperationException("Kaynak ledger hesabı bulunamadı.");
        var destination = await unitOfWork.LedgerAccounts.GetBySourceAccountIdAsync(
                request.DestinationAccountId,
                cancellationToken)
            ?? throw new InvalidOperationException("Hedef ledger hesabı bulunamadı.");

        if (source.Kind != ELedgerAccountKind.CustomerDemandDeposit ||
            destination.Kind != ELedgerAccountKind.CustomerDemandDeposit)
            throw new InvalidOperationException("Çevrim yalnızca vadesiz müşteri hesapları arasında yapılır.");

        if (source.Status != ELedgerAccountStatus.Active || destination.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        if (source.CustomerId != destination.CustomerId)
            throw new InvalidOperationException("Çevrim yalnızca aynı müşterinin hesapları arasında yapılır.");

        if (!string.Equals(source.Currency, request.SourceCurrency, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(destination.Currency, request.DestinationCurrency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Para birimi uyuşmuyor.");

        var sellAmount = decimal.Round(request.SourceAmount, 4, MidpointRounding.AwayFromZero);
        var buyAmount = decimal.Round(request.DestinationAmount, 4, MidpointRounding.AwayFromZero);

        var sourceLines = await unitOfWork.JournalEntries.GetLinesByLedgerAccountIdAsync(source.Id, cancellationToken);
        var sourceHolds = await unitOfWork.Holds.GetActiveByLedgerAccountIdAsync(source.Id, cancellationToken);
        var (_, _, available) = LedgerBalanceCalculator.Calculate(source.Kind, sourceLines, sourceHolds);
        if (sellAmount > available)
            throw new InvalidOperationException("Available yetersiz.");

        var sourceClearing = await unitOfWork.LedgerAccounts.GetByIdAsync(
                SystemLedgerAccounts.ClearingFor(source.Currency))
            ?? throw new InvalidOperationException("Kaynak clearing hesabı bulunamadı.");
        var destClearing = await unitOfWork.LedgerAccounts.GetByIdAsync(
                SystemLedgerAccounts.ClearingFor(destination.Currency))
            ?? throw new InvalidOperationException("Hedef clearing hesabı bulunamadı.");

        if (sourceClearing.Status != ELedgerAccountStatus.Active || destClearing.Status != ELedgerAccountStatus.Active)
            throw new InvalidOperationException("Pasif hesaba posting yapılamaz.");

        JournalEntry sell;
        if (existingSell is null)
        {
            sell = new JournalEntry
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = sellKey,
                Description = "Döviz satış",
                BookedAt = DateTimeOffset.UtcNow,
                Lines =
                [
                    new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        LedgerAccountId = source.Id,
                        Side = EEntrySide.Debit,
                        Amount = sellAmount
                    },
                    new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        LedgerAccountId = sourceClearing.Id,
                        Side = EEntrySide.Credit,
                        Amount = sellAmount
                    }
                ]
            };
            await unitOfWork.JournalEntries.AddAsync(sell, cancellationToken);
        }
        else
        {
            sell = existingSell;
        }

        JournalEntry buy;
        if (existingBuy is null)
        {
            buy = new JournalEntry
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = buyKey,
                Description = "Döviz alış",
                BookedAt = DateTimeOffset.UtcNow,
                Lines =
                [
                    new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        LedgerAccountId = destClearing.Id,
                        Side = EEntrySide.Debit,
                        Amount = buyAmount
                    },
                    new JournalLine
                    {
                        Id = Guid.NewGuid(),
                        LedgerAccountId = destination.Id,
                        Side = EEntrySide.Credit,
                        Amount = buyAmount
                    }
                ]
            };
            await unitOfWork.JournalEntries.AddAsync(buy, cancellationToken);
        }
        else
        {
            buy = existingBuy;
        }

        await eventPublisher.PublishAsync(
            new FxConversionCompleted(request.ConversionId, sell.Id, buy.Id, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ApplyFxConversionResponse
        {
            Message = "Döviz çevrimi kaydedildi.",
            SellJournalEntryId = sell.Id,
            BuyJournalEntryId = buy.Id
        };
    }

    public static string SellJournalKey(Guid conversionId) => $"fx-sell:{conversionId:D}";

    public static string BuyJournalKey(Guid conversionId) => $"fx-buy:{conversionId:D}";
}
