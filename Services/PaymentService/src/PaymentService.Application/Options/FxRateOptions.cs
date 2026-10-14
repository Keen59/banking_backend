namespace PaymentService.Application.Options;

public class FxRateOptions
{
    public const string SectionName = "FxRates";

    public decimal UsdTry { get; set; } = 34m;

    public decimal EurTry { get; set; } = 37m;
}
