using Banking.Contracts.Events;

namespace LoanService.Application.Helpers;

public static class CustomerNotifications
{
    public static NotificationRequested LoanRejected(Guid customerId, Guid sourceId, string reason, DateTimeOffset at)
        => Build(customerId, "loan.rejected", "Kredi reddedildi", reason, sourceId, at);

    public static NotificationRequested LoanDisbursed(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(
            customerId,
            "loan.disbursed",
            "Kredi kullandırıldı",
            $"{Format(amount)} TRY kredi tutarı hesabınıza yatırıldı.",
            sourceId,
            at);

    public static NotificationRequested LoanRepaid(Guid customerId, Guid sourceId, decimal amount, DateTimeOffset at)
        => Build(
            customerId,
            "loan.repaid",
            "Kredi taksiti ödendi",
            $"{Format(amount)} TRY kredi taksiti tahsil edildi.",
            sourceId,
            at);

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
