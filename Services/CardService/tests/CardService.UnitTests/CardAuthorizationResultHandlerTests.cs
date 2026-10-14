using Banking.Contracts.Events;
using CardService.Application.Commands.CaptureCardAuthorization;
using CardService.Application.Commands.CompleteAuthorization;
using CardService.Application.Commands.MarkAuthorizationHeld;
using CardService.Application.Commands.RejectAuthorization;
using CardService.Application.Commands.ReverseCardAuthorization;
using CardService.Domain.Entities;
using CardService.Domain.Enums;
using Xunit;

namespace CardService.UnitTests;

public class CardAuthorizationResultHandlerTests
{
    [Fact]
    public async Task Capture_from_held_publishes_CardCaptureRequested()
    {
        var (store, card, authorization) = SeededHeld();
        var publisher = new RecordingPublisher();

        var response = await new CaptureCardAuthorizationHandler(store, publisher).Handle(
            new CaptureCardAuthorizationCommand
            {
                CustomerId = card.CustomerId,
                CardId = card.Id,
                AuthorizationId = authorization.Id
            },
            CancellationToken.None);

        Assert.Equal(EAuthorizationStatus.Held, response.Data!.Status);
        var published = Assert.Single(publisher.Messages.OfType<CardCaptureRequested>());
        Assert.Equal(authorization.Id, published.AuthorizationId);
    }

    [Fact]
    public async Task Capture_from_initiated_throws()
    {
        var (store, card, authorization) = SeededInitiated();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CaptureCardAuthorizationHandler(store, new RecordingPublisher()).Handle(
                new CaptureCardAuthorizationCommand
                {
                    CustomerId = card.CustomerId,
                    CardId = card.Id,
                    AuthorizationId = authorization.Id
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task Reverse_from_held_publishes_CardAuthorizationReverseRequested()
    {
        var (store, card, authorization) = SeededHeld();
        var publisher = new RecordingPublisher();

        await new ReverseCardAuthorizationHandler(store, publisher).Handle(
            new ReverseCardAuthorizationCommand
            {
                CustomerId = card.CustomerId,
                CardId = card.Id,
                AuthorizationId = authorization.Id,
                Reason = "iptal"
            },
            CancellationToken.None);

        var published = Assert.Single(publisher.Messages.OfType<CardAuthorizationReverseRequested>());
        Assert.Equal(authorization.Id, published.AuthorizationId);
        Assert.Equal("iptal", published.Reason);
    }

    [Fact]
    public async Task Reverse_from_completed_throws()
    {
        var (store, card, authorization) = SeededHeld();
        authorization.Status = EAuthorizationStatus.Completed;
        authorization.JournalEntryId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ReverseCardAuthorizationHandler(store, new RecordingPublisher()).Handle(
                new ReverseCardAuthorizationCommand
                {
                    CustomerId = card.CustomerId,
                    CardId = card.Id,
                    AuthorizationId = authorization.Id
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task Hold_sets_held_from_initiated()
    {
        var (store, _, authorization) = SeededInitiated();
        var publisher = new RecordingPublisher();

        await new MarkAuthorizationHeldHandler(store, publisher).Handle(new MarkAuthorizationHeldCommand
        {
            Event = new CardAuthorizationHeld(authorization.Id, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(EAuthorizationStatus.Held, authorization.Status);
        var notice = Assert.Single(publisher.Messages.OfType<NotificationRequested>());
        Assert.Equal("card.held", notice.Type);
    }

    [Fact]
    public async Task Hold_does_not_overwrite_completed()
    {
        var (store, _, authorization) = SeededInitiated();
        authorization.Status = EAuthorizationStatus.Completed;
        authorization.JournalEntryId = Guid.NewGuid();

        await new MarkAuthorizationHeldHandler(store, new RecordingPublisher()).Handle(new MarkAuthorizationHeldCommand
        {
            Event = new CardAuthorizationHeld(authorization.Id, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(EAuthorizationStatus.Completed, authorization.Status);
    }

    [Fact]
    public async Task Complete_sets_journal_and_completed_status()
    {
        var (store, _, authorization) = SeededHeld();
        var journalId = Guid.NewGuid();
        var publisher = new RecordingPublisher();

        await new CompleteAuthorizationHandler(store, publisher).Handle(new CompleteAuthorizationCommand
        {
            Event = new CardAuthorizationCompleted(authorization.Id, journalId, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(EAuthorizationStatus.Completed, authorization.Status);
        Assert.Equal(journalId, authorization.JournalEntryId);
        Assert.Null(authorization.RejectReason);
        var notice = Assert.Single(publisher.Messages.OfType<NotificationRequested>());
        Assert.Equal("card.completed", notice.Type);
    }

    [Fact]
    public async Task Complete_does_not_overwrite_completed()
    {
        var (store, _, authorization) = SeededHeld();
        var originalJournal = Guid.NewGuid();
        authorization.Status = EAuthorizationStatus.Completed;
        authorization.JournalEntryId = originalJournal;
        var publisher = new RecordingPublisher();

        await new CompleteAuthorizationHandler(store, publisher).Handle(new CompleteAuthorizationCommand
        {
            Event = new CardAuthorizationCompleted(authorization.Id, Guid.NewGuid(), DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(EAuthorizationStatus.Completed, authorization.Status);
        Assert.Equal(originalJournal, authorization.JournalEntryId);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task Reject_sets_reason_when_not_completed()
    {
        var (store, _, authorization) = SeededHeld();
        var publisher = new RecordingPublisher();

        await new RejectAuthorizationHandler(store, publisher).Handle(new RejectAuthorizationCommand
        {
            Event = new CardAuthorizationRejected(authorization.Id, "yetersiz bakiye", DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(EAuthorizationStatus.Rejected, authorization.Status);
        Assert.Equal("yetersiz bakiye", authorization.RejectReason);
        var notice = Assert.Single(publisher.Messages.OfType<NotificationRequested>());
        Assert.Equal("card.rejected", notice.Type);
    }

    [Fact]
    public async Task Reject_does_not_change_completed_authorization()
    {
        var (store, _, authorization) = SeededHeld();
        authorization.Status = EAuthorizationStatus.Completed;
        authorization.JournalEntryId = Guid.NewGuid();

        await new RejectAuthorizationHandler(store, new RecordingPublisher()).Handle(new RejectAuthorizationCommand
        {
            Event = new CardAuthorizationRejected(authorization.Id, "late reject", DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(EAuthorizationStatus.Completed, authorization.Status);
        Assert.Null(authorization.RejectReason);
    }

    private static (InMemoryCardUnitOfWork Store, Card Card, CardAuthorization Authorization) SeededInitiated()
    {
        var card = new Card
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Token = Guid.NewGuid().ToString("N"),
            Last4 = "1111",
            Brand = "Virtual",
            Status = ECardStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var authorization = new CardAuthorization
        {
            Id = Guid.NewGuid(),
            CardId = card.Id,
            CustomerId = card.CustomerId,
            AccountId = card.AccountId,
            Amount = 25m,
            Currency = "TRY",
            Merchant = "Market",
            Description = "Kart",
            IdempotencyKey = "k",
            Status = EAuthorizationStatus.Initiated,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var store = new InMemoryCardUnitOfWork();
        store.CardRows.Add(card);
        store.AuthorizationRows.Add(authorization);
        return (store, card, authorization);
    }

    private static (InMemoryCardUnitOfWork Store, Card Card, CardAuthorization Authorization) SeededHeld()
    {
        var seeded = SeededInitiated();
        seeded.Authorization.Status = EAuthorizationStatus.Held;
        return seeded;
    }
}
