namespace RiskService.Application.Options;

public class RiskLimitOptions
{
    public const string SectionName = "RiskLimits";

    public decimal FlagAmount { get; set; } = 25_000m;

    public decimal DailyAmount { get; set; } = 80_000m;

    public decimal StructuringFloor { get; set; } = 20_000m;

    public int StructuringCount { get; set; } = 3;
}
