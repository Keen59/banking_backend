using PaymentService.Application.Interfaces.Repositories;

namespace PaymentService.Application.Helpers;

public static class PaymentDailyLimit
{
    public static DateTimeOffset UtcDayStart()
        => new(DateTime.UtcNow.Date, TimeSpan.Zero);

    public static async Task<decimal> SumCountedAsync(
        IUnitOfWork unitOfWork,
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var from = UtcDayStart();
        var transfers = await unitOfWork.Transfers.SumCountedTowardDailyLimitAsync(
            customerId,
            from,
            cancellationToken);
        var fast = await unitOfWork.FastPayments.SumCountedTowardDailyLimitAsync(
            customerId,
            from,
            cancellationToken);
        var eft = await unitOfWork.EftPayments.SumCountedTowardDailyLimitAsync(
            customerId,
            from,
            cancellationToken);
        return transfers + fast + eft;
    }
}
