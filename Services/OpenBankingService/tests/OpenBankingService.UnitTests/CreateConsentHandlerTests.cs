using Microsoft.Extensions.Options;
using OpenBankingService.Application.Commands.CreateConsent;
using OpenBankingService.Application.Options;
using OpenBankingService.Domain.Entities;
using OpenBankingService.Domain.Enums;
using Xunit;

namespace OpenBankingService.UnitTests;

public class CreateConsentHandlerTests
{
    [Fact]
    public async Task Handle_creates_active_consent_with_32_hex_token_and_publishes_nothing()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, customerId);
        var publisher = new RecordingPublisher();
        var handler = new CreateConsentHandler(store, Options());

        var response = await handler.Handle(new CreateConsentCommand
        {
            CustomerId = customerId,
            TppClientId = "sandbox-tpp",
            Scopes = (int)(EConsentScope.Ais | EConsentScope.Pis),
            AccountIds = [accountId],
            ExpiresInDays = 90,
            IdempotencyKey = "consent-1"
        }, CancellationToken.None);

        Assert.Equal(EConsentStatus.Active, response.Data!.Status);
        Assert.Equal(32, response.Data.AccessToken.Length);
        Assert.True(response.Data.AccessToken.All(Uri.IsHexDigit));
        Assert.Single(store.ConsentRows);
        Assert.Empty(publisher.Messages);
        Assert.Empty(store.InitiationRows);
    }

    [Fact]
    public async Task Handle_returns_existing_consent_for_same_idempotency_key()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, customerId);
        var handler = new CreateConsentHandler(store, Options());
        var command = new CreateConsentCommand
        {
            CustomerId = customerId,
            TppClientId = "sandbox-tpp",
            Scopes = (int)EConsentScope.Ais,
            AccountIds = [accountId],
            ExpiresInDays = 30,
            IdempotencyKey = "same-key"
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.ConsentRows);
    }

    [Fact]
    public async Task Handle_rejects_unknown_tpp()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, customerId);
        var handler = new CreateConsentHandler(store, Options());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateConsentCommand
            {
                CustomerId = customerId,
                TppClientId = "unknown-tpp",
                Scopes = (int)EConsentScope.Ais,
                AccountIds = [accountId],
                ExpiresInDays = 90,
                IdempotencyKey = "unknown-tpp-key"
            }, CancellationToken.None));

        Assert.Contains("Unknown TPP", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.ConsentRows);
    }

    [Fact]
    public async Task Handle_rejects_account_not_owned_by_caller()
    {
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, Guid.NewGuid());
        var handler = new CreateConsentHandler(store, Options());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateConsentCommand
            {
                CustomerId = Guid.NewGuid(),
                TppClientId = "sandbox-tpp",
                Scopes = (int)EConsentScope.Ais,
                AccountIds = [accountId],
                ExpiresInDays = 90,
                IdempotencyKey = "foreign-account"
            }, CancellationToken.None));

        Assert.Empty(store.ConsentRows);
    }

    internal static InMemoryOpenBankingUnitOfWork SeededStore(Guid accountId, Guid ownerId, string iban = "TR330006100519786457841326")
    {
        var store = new InMemoryOpenBankingUnitOfWork();
        store.Accounts.Add(new AccountProjection
        {
            Id = accountId,
            CustomerId = ownerId,
            Iban = iban,
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });
        return store;
    }

    internal static IOptions<OpenBankingOptions> Options()
        => Microsoft.Extensions.Options.Options.Create(new OpenBankingOptions
        {
            AllowedTppClientIds = ["sandbox-tpp"]
        });
}
