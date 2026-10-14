using OpenBankingService.Application.Commands.GetAisAccounts;
using OpenBankingService.Domain.Entities;
using OpenBankingService.Domain.Enums;
using Xunit;

namespace OpenBankingService.UnitTests;

public class AisHandlerTests
{
    [Fact]
    public async Task Handle_returns_consented_accounts_for_valid_ais_token()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = CreateConsentHandlerTests.SeededStore(accountId, customerId);
        var consent = ActiveConsent(customerId, accountId, (int)EConsentScope.Ais);
        store.ConsentRows.Add(consent);

        var response = await new GetAisAccountsHandler(store).Handle(
            new GetAisAccountsCommand { AccessToken = consent.AccessToken },
            CancellationToken.None);

        var account = Assert.Single(response.Data);
        Assert.Equal(accountId, account.AccountId);
        Assert.Equal("TR330006100519786457841326", account.Iban);
        Assert.Equal("TRY", account.Currency);
    }

    [Fact]
    public async Task Handle_rejects_revoked_consent_for_ais()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = CreateConsentHandlerTests.SeededStore(accountId, customerId);
        var consent = ActiveConsent(customerId, accountId, (int)EConsentScope.Ais);
        consent.Status = EConsentStatus.Revoked;
        store.ConsentRows.Add(consent);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GetAisAccountsHandler(store).Handle(
                new GetAisAccountsCommand { AccessToken = consent.AccessToken },
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_rejects_expired_consent_for_ais()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = CreateConsentHandlerTests.SeededStore(accountId, customerId);
        var consent = ActiveConsent(customerId, accountId, (int)EConsentScope.Ais);
        consent.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        store.ConsentRows.Add(consent);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GetAisAccountsHandler(store).Handle(
                new GetAisAccountsCommand { AccessToken = consent.AccessToken },
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_rejects_missing_consent_token_for_ais()
    {
        var store = new InMemoryOpenBankingUnitOfWork();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new GetAisAccountsHandler(store).Handle(
                new GetAisAccountsCommand { AccessToken = Guid.NewGuid().ToString("N") },
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_rejects_pis_only_consent_for_ais()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = CreateConsentHandlerTests.SeededStore(accountId, customerId);
        var consent = ActiveConsent(customerId, accountId, (int)EConsentScope.Pis);
        store.ConsentRows.Add(consent);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GetAisAccountsHandler(store).Handle(
                new GetAisAccountsCommand { AccessToken = consent.AccessToken },
                CancellationToken.None));
    }

    internal static Consent ActiveConsent(Guid customerId, Guid accountId, int scopes)
    {
        var consentId = Guid.NewGuid();
        return new Consent
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
                new ConsentAccount
                {
                    Id = Guid.NewGuid(),
                    ConsentId = consentId,
                    AccountId = accountId
                }
            ]
        };
    }
}
