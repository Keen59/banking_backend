using Banking.Contracts.Events;

namespace CardService.Application.Helpers;

public static class CustomerNotifications
{
    public static NotificationRequested CardIssued(Guid customerId, Guid sourceId, string last4, DateTimeOffset at)
        => Build(customerId, "card.issued", "Sanal kart açıldı", last4, sourceId, at);

    public static NotificationRequested CardHeld(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "card.held", "Kart yetkisi bekliyor", $"{Format(amount)} TRY kart işleminiz bekliyor.", sourceId, at);

    public static NotificationRequested CardCompleted(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "card.completed", "Kart işlemi tamamlandı", $"{Format(amount)} TRY kart işleminiz tamamlandı.", sourceId, at);

    public static NotificationRequested CardRejected(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "card.rejected", "Kart işlemi reddedildi", $"{Format(amount)} TRY kart işleminiz reddedildi.", sourceId, at);

    private static NotificationRequested Build(
        Guid customerId,
        string type,
        string title,
        string body,
        Guid sourceId,
        DateTimeOffset at)
        => new(customerId, type, title, body, sourceId, at);

    private static string Format(decimal amount) => amount.ToString("0.##");
}
