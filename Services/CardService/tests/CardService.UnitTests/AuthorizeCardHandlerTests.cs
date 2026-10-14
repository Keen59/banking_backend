using Banking.Contracts.Events;
using CardService.Application.Commands.AuthorizeCard;
using CardService.Domain.Entities;
using CardService.Domain.Enums;
using Xunit;

namespace CardService.UnitTests;

public class AuthorizeCardHandlerTests
{
    [Fact]
    public async Task Handle_publishes_CardAuthorizationRequested_as_initiated()
    {
        var card = SeededCard();
        var store = new InMemoryCardUnitOfWork();
        store.CardRows.Add(card);
        var publisher = new RecordingPublisher();
        var handler = new AuthorizeCardHandler(store, publisher);

        var response = await handler.Handle(Command(card), CancellationToken.None);

        Assert.NotNull(response.Data);
        Assert.Equal(EAuthorizationStatus.Initiated, response.Data!.Status);
        var published = Assert.Single(publisher.Messages.OfType<CardAuthorizationRequested>());
        Assert.Equal(response.Data.Id, published.AuthorizationId);
        Assert.Equal(card.AccountId, published.AccountId);
        Assert.Equal(100m, published.Amount);
        Assert.Equal("TRY", published.Currency);
        Assert.Equal("Market", published.Merchant);
    }

    [Fact]
    public async Task Handle_same_idempotency_key_returns_existing_without_second_publish()
    {
        var card = SeededCard();
        var store = new InMemoryCardUnitOfWork();
        store.CardRows.Add(card);
        var publisher = new RecordingPublisher();
        var handler = new AuthorizeCardHandler(store, publisher);
        var command = Command(card);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.AuthorizationRows);
        Assert.Single(publisher.Messages.OfType<CardAuthorizationRequested>());
    }

    [Fact]
    public async Task Handle_frozen_card_throws()
    {
        var card = SeededCard();
        card.Status = ECardStatus.Frozen;
        var store = new InMemoryCardUnitOfWork();
        store.CardRows.Add(card);
        var handler = new AuthorizeCardHandler(store, new RecordingPublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(Command(card), CancellationToken.None));

        Assert.Empty(store.AuthorizationRows);
    }

    [Fact]
    public async Task Handle_amount_above_50000_throws()
    {
        var card = SeededCard();
        var store = new InMemoryCardUnitOfWork();
        store.CardRows.Add(card);
        var handler = new AuthorizeCardHandler(store, new RecordingPublisher());
        var command = Command(card);
        command.Amount = 50_000.01m;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Empty(store.AuthorizationRows);
    }

    private static Card SeededCard() => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        AccountId = Guid.NewGuid(),
        Token = Guid.NewGuid().ToString("N"),
        Last4 = "4242",
        Brand = "Virtual",
        Status = ECardStatus.Active,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static AuthorizeCardCommand Command(Card card) => new()
    {
        CustomerId = card.CustomerId,
        CardId = card.Id,
        Amount = 100m,
        Currency = "TRY",
        Merchant = "Market",
        Description = "Kart yetkisi",
        IdempotencyKey = "auth-1"
    };
}
