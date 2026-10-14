using System.Text.RegularExpressions;
using Banking.Contracts.Events;
using CardService.Application.Commands.IssueCardFromAccountOpened;
using CardService.Domain.Entities;
using CardService.Domain.Enums;
using Xunit;

namespace CardService.UnitTests;

public class IssueCardFromAccountOpenedHandlerTests
{
    [Fact]
    public async Task Handle_issues_one_active_virtual_card_and_does_not_duplicate()
    {
        var accountId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var store = new InMemoryCardUnitOfWork();
        var publisher = new RecordingPublisher();
        var handler = new IssueCardFromAccountOpenedHandler(store, publisher);
        var command = new IssueCardFromAccountOpenedCommand
        {
            Event = new AccountOpened(
                accountId,
                customerId,
                "000000000001",
                "TR330010012345678901234567",
                "TRY",
                DateTimeOffset.UtcNow)
        };

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        var card = Assert.Single(store.CardRows);
        Assert.Equal(accountId, card.AccountId);
        Assert.Equal(customerId, card.CustomerId);
        Assert.Equal(ECardStatus.Active, card.Status);
        Assert.Equal("Virtual", card.Brand);
        Assert.Single(publisher.Messages.OfType<NotificationRequested>());
    }

    [Fact]
    public async Task Issued_token_is_32_hex_chars_last4_is_4_digits_and_card_has_no_pan()
    {
        var store = new InMemoryCardUnitOfWork();
        var handler = new IssueCardFromAccountOpenedHandler(store, new RecordingPublisher());

        await handler.Handle(new IssueCardFromAccountOpenedCommand
        {
            Event = new AccountOpened(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "000000000001",
                "TR330010012345678901234567",
                "TRY",
                DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        var card = Assert.Single(store.CardRows);
        Assert.Equal(32, card.Token.Length);
        Assert.Matches(new Regex("^[0-9a-f]{32}$"), card.Token);
        Assert.Equal(4, card.Last4.Length);
        Assert.Matches(new Regex("^\\d{4}$"), card.Last4);

        var names = typeof(Card).GetProperties().Select(p => p.Name).ToList();
        Assert.DoesNotContain("Pan", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("CardNumber", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("PrimaryAccountNumber", names, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain(names, name => name.Contains("Pan", StringComparison.OrdinalIgnoreCase));
    }
}
