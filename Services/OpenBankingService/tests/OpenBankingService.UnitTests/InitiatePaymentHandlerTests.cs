using Banking.Contracts.Events;
using OpenBankingService.Application.Commands.InitiatePayment;
using OpenBankingService.Application.Commands.RevokeConsent;
using OpenBankingService.Domain.Entities;
using OpenBankingService.Domain.Enums;
using Xunit;

namespace OpenBankingService.UnitTests;

public class InitiatePaymentHandlerTests
{
    [Fact]
    public async Task Handle_publishes_open_banking_payment_requested_with_initiation_id()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var (store, consent) = SeededConsent(customerId, sourceId, destId, (int)(EConsentScope.Ais | EConsentScope.Pis));
        var publisher = new RecordingPublisher();

        var response = await new InitiatePaymentHandler(store, publisher).Handle(
            PaymentCommand(consent.AccessToken, sourceId, destId),
            CancellationToken.None);

        Assert.Equal(EInitiationStatus.Initiated, response.Data!.Status);
        var published = Assert.Single(publisher.Messages.OfType<OpenBankingPaymentRequested>());
        Assert.Equal(response.Data.Id, published.InitiationId);
        Assert.Equal(customerId, published.CustomerId);
        Assert.Equal(sourceId, published.SourceAccountId);
        Assert.Equal(destId, published.DestinationAccountId);
        Assert.Equal(25m, published.Amount);
        Assert.Equal("TRY", published.Currency);
    }

    [Fact]
    public async Task Handle_rejects_consent_without_pis_scope()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var (store, consent) = SeededConsent(customerId, sourceId, destId, (int)EConsentScope.Ais);
        var publisher = new RecordingPublisher();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new InitiatePaymentHandler(store, publisher).Handle(
                PaymentCommand(consent.AccessToken, sourceId, destId),
                CancellationToken.None));

        Assert.Empty(store.InitiationRows);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task Handle_rejects_pis_after_revoke()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var (store, consent) = SeededConsent(customerId, sourceId, destId, (int)EConsentScope.Pis);

        await new RevokeConsentHandler(store).Handle(
            new RevokeConsentCommand { ConsentId = consent.Id, CustomerId = customerId },
            CancellationToken.None);

        Assert.Equal(EConsentStatus.Revoked, consent.Status);

        var publisher = new RecordingPublisher();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new InitiatePaymentHandler(store, publisher).Handle(
                PaymentCommand(consent.AccessToken, sourceId, destId),
                CancellationToken.None));

        Assert.Empty(store.InitiationRows);
        Assert.Empty(publisher.Messages);
    }

    private static (InMemoryOpenBankingUnitOfWork Store, Consent Consent) SeededConsent(
        Guid customerId,
        Guid sourceId,
        Guid destId,
        int scopes)
    {
        var store = CreateConsentHandlerTests.SeededStore(sourceId, customerId);
        store.Accounts.Add(new AccountProjection
        {
            Id = destId,
            CustomerId = customerId,
            Iban = "TR470006100519786457841327",
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });

        var consentId = Guid.NewGuid();
        var consent = new Consent
        {
            Id = consentId,
            CustomerId = customerId,
            TppClientId = "sandbox-tpp",
            AccessToken = Guid.NewGuid().ToString("N"),
            Scopes = scopes,
            Status = EConsentStatus.Active,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Accounts =
            [
                new ConsentAccount { Id = Guid.NewGuid(), ConsentId = consentId, AccountId = sourceId },
                new ConsentAccount { Id = Guid.NewGuid(), ConsentId = consentId, AccountId = destId }
            ]
        };
        store.ConsentRows.Add(consent);
        return (store, consent);
    }

    private static InitiatePaymentCommand PaymentCommand(string token, Guid sourceId, Guid destId)
        => new()
        {
            AccessToken = token,
            SourceAccountId = sourceId,
            DestinationAccountId = destId,
            Amount = 25m,
            Currency = "TRY",
            Description = "Virman",
            IdempotencyKey = "pis-1"
        };
}
