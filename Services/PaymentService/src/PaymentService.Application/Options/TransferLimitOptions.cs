namespace PaymentService.Application.Options;

public class TransferLimitOptions
{
    public const string SectionName = "TransferLimits";

    public decimal MaxAmount { get; set; } = 50_000m;

    public decimal DailyAmount { get; set; } = 100_000m;

    public decimal MaxTestCredit { get; set; } = 100_000m;
}
