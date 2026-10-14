using Banking.Contracts.Events;
using Microsoft.Extensions.Options;
using PaymentService.Application.Commands.CompleteFxConversion;
using PaymentService.Application.Commands.CreateFxConversion;
using PaymentService.Application.Helpers;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class CreateFxConversionHandlerTests
{
    [Fact]
    public async Task Handle_initiates_try_to_usd_and_publishes_requested()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId, destinationId);
        var publisher = new RecordingPublisher();
        var handler = new CreateFxConversionHandler(store, publisher, Limits(), Rates());

        var response = await handler.Handle(new CreateFxConversionCommand
        {
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationAccountId = destinationId,
            SourceAmount = 3400m,
            Description = "USD al",
            IdempotencyKey = "fx-1"
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Initiated, response.Data!.Status);
        Assert.Equal("TRY", response.Data.SourceCurrency);
        Assert.Equal("USD", response.Data.DestinationCurrency);
        Assert.Equal(100m, response.Data.DestinationAmount);
        Assert.Equal(3400m, response.Data.TryEquivalent);
        var published = Assert.Single(publisher.Messages.OfType<FxConversionRequested>());
        Assert.Equal(100m, published.DestinationAmount);
        Assert.Equal(3400m, published.TryEquivalent);
    }

    [Fact]
    public async Task Handle_same_idempotency_key_returns_existing()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId, destinationId);
        var publisher = new RecordingPublisher();
        var handler = new CreateFxConversionHandler(store, publisher, Limits(), Rates());
        var command = new CreateFxConversionCommand
        {
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationAccountId = destinationId,
            SourceAmount = 34m,
            IdempotencyKey = "fx-dup"
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.FxConversionRows);
        Assert.Single(publisher.Messages.OfType<FxConversionRequested>());
    }

    [Fact]
    public async Task Handle_rejects_same_currency()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var otherTry = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId, Guid.NewGuid());
        store.Accounts.Add(new AccountProjection
        {
            Id = otherTry,
            CustomerId = customerId,
            Iban = "TR330010011111111111111111",
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });
        var handler = new CreateFxConversionHandler(store, new RecordingPublisher(), Limits(), Rates());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateFxConversionCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationAccountId = otherTry,
                SourceAmount = 10m,
                IdempotencyKey = "fx-same"
            }, CancellationToken.None));

        Assert.Contains("transfers", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_rejects_when_destination_belongs_to_another_customer()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var foreignUsd = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId, Guid.NewGuid());
        store.Accounts[1].Id = foreignUsd;
        store.Accounts[1].CustomerId = Guid.NewGuid();
        var handler = new CreateFxConversionHandler(store, new RecordingPublisher(), Limits(), Rates());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateFxConversionCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationAccountId = foreignUsd,
                SourceAmount = 34m,
                IdempotencyKey = "fx-foreign"
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Complete_handler_publishes_notification_and_does_not_overwrite_completed()
    {
        var store = new InMemoryPaymentUnitOfWork();
        var conversion = new FxConversion
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            SourceAmount = 34m,
            SourceCurrency = "TRY",
            DestinationAmount = 1m,
            DestinationCurrency = "USD",
            Status = ETransferStatus.Initiated
        };
        store.FxConversionRows.Add(conversion);
        var publisher = new RecordingPublisher();
        var handler = new CompleteFxConversionHandler(store, publisher);
        var completed = new FxConversionCompleted(
            conversion.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        await handler.Handle(new CompleteFxConversionCommand { Event = completed }, CancellationToken.None);
        await handler.Handle(new CompleteFxConversionCommand { Event = completed }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, conversion.Status);
        Assert.Single(publisher.Messages.OfType<NotificationRequested>());
    }

    [Fact]
    public void Converter_try_to_usd_and_usd_to_try()
    {
        var rates = new FxRateOptions { UsdTry = 34m, EurTry = 37m };
        Assert.Equal(100m, FxConverter.Convert(3400m, "TRY", "USD", rates));
        Assert.Equal(3400m, FxConverter.Convert(100m, "USD", "TRY", rates));
        Assert.Equal(100m, FxConverter.Convert(3700m, "TRY", "EUR", rates));
    }

    private static InMemoryPaymentUnitOfWork SeededStore(Guid customerId, Guid tryId, Guid usdId)
    {
        var store = new InMemoryPaymentUnitOfWork();
        store.Accounts.Add(new AccountProjection
        {
            Id = tryId,
            CustomerId = customerId,
            Iban = "TR330010012345678901234567",
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });
        store.Accounts.Add(new AccountProjection
        {
            Id = usdId,
            CustomerId = customerId,
            Iban = "TR330010098765432109876543",
            Currency = "USD",
            Status = EAccountProjectionStatus.Active
        });
        return store;
    }

    private static IOptions<TransferLimitOptions> Limits()
        => Options.Create(new TransferLimitOptions
        {
            MaxAmount = 50_000m,
            DailyAmount = 100_000m
        });

    private static IOptions<FxRateOptions> Rates()
        => Options.Create(new FxRateOptions
        {
            UsdTry = 34m,
            EurTry = 37m
        });
}
