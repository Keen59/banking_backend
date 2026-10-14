namespace LoanService.Application.Options;

public class LoanLimitOptions
{
    public const string SectionName = "LoanLimits";

    public decimal MaxAmount { get; set; } = 100_000m;
}
