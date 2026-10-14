using Banking.Contracts.Events;

namespace PaymentService.Application.Helpers;

public static class CustomerNotifications
{
    public static NotificationRequested TransferCompleted(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "transfer.completed", "Virman tamamlandı", $"{Format(amount)} TRY virman işleminiz tamamlandı.", sourceId, at);

    public static NotificationRequested TransferRejected(Guid customerId, Guid sourceId, string reason, DateTimeOffset at)
        => Build(customerId, "transfer.rejected", "Virman reddedildi", reason, sourceId, at);

    public static NotificationRequested FastCompleted(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "fast.completed", "FAST tamamlandı", $"{Format(amount)} TRY FAST işleminiz tamamlandı.", sourceId, at);

    public static NotificationRequested FastRejected(Guid customerId, Guid sourceId, string reason, DateTimeOffset at)
        => Build(customerId, "fast.rejected", "FAST reddedildi", reason, sourceId, at);

    public static NotificationRequested EftHeld(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "eft.held", "EFT bekliyor", $"{Format(amount)} TRY EFT işleminiz takas bekliyor.", sourceId, at);

    public static NotificationRequested EftCompleted(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "eft.completed", "EFT tamamlandı", $"{Format(amount)} TRY EFT işleminiz tamamlandı.", sourceId, at);

    public static NotificationRequested EftRejected(Guid customerId, Guid sourceId, string reason, DateTimeOffset at)
        => Build(customerId, "eft.rejected", "EFT reddedildi", reason, sourceId, at);

    public static NotificationRequested IncomingFastCompleted(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "incoming-fast.completed", "Gelen FAST", $"{Format(amount)} TRY gelen FAST hesabınıza yattı.", sourceId, at);

    public static NotificationRequested IncomingFastRejected(Guid customerId, Guid sourceId, string reason, DateTimeOffset at)
        => Build(customerId, "incoming-fast.rejected", "Gelen FAST reddedildi", reason, sourceId, at);

    public static NotificationRequested IncomingEftHeld(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "incoming-eft.held", "Gelen EFT bekliyor", $"{Format(amount)} TRY gelen EFT takas bekliyor.", sourceId, at);

    public static NotificationRequested IncomingEftCompleted(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "incoming-eft.completed", "Gelen EFT", $"{Format(amount)} TRY gelen EFT hesabınıza yattı.", sourceId, at);

    public static NotificationRequested IncomingEftRejected(Guid customerId, Guid sourceId, string reason, DateTimeOffset at)
        => Build(customerId, "incoming-eft.rejected", "Gelen EFT reddedildi", reason, sourceId, at);

    public static NotificationRequested TestCreditPosted(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(customerId, "test-credit.posted", "Test kredisi", $"{Format(amount)} TRY test kredisi hesabınıza yattı.", sourceId, at);

    public static NotificationRequested TestCreditRejected(Guid customerId, Guid sourceId, string reason, DateTimeOffset at)
        => Build(customerId, "test-credit.rejected", "Test kredisi reddedildi", reason, sourceId, at);

    public static NotificationRequested FxCompleted(
        Guid customerId,
        Guid sourceId,
        decimal sourceAmount,
        string sourceCurrency,
        decimal destinationAmount,
        string destinationCurrency,
        DateTimeOffset at)
        => Build(
            customerId,
            "fx.completed",
            "Döviz çevrimi tamamlandı",
            $"{Format(sourceAmount)} {sourceCurrency} → {Format(destinationAmount)} {destinationCurrency} çevrimi tamamlandı.",
            sourceId,
            at);

    public static NotificationRequested FxRejected(Guid customerId, Guid sourceId, string reason, DateTimeOffset at)
        => Build(customerId, "fx.rejected", "Döviz çevrimi reddedildi", reason, sourceId, at);

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
