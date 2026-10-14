using PaymentService.Application.Options;

namespace PaymentService.Application.Helpers;

public static class FxConverter
{
    public static string Normalize(string currency) => currency.Trim().ToUpperInvariant();

    public static bool IsSupported(string currency)
    {
        var code = Normalize(currency);
        return code is "TRY" or "USD" or "EUR";
    }

    public static decimal ToTry(decimal amount, string currency, FxRateOptions rates)
    {
        var rounded = decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
        return Normalize(currency) switch
        {
            "TRY" => rounded,
            "USD" => decimal.Round(rounded * rates.UsdTry, 4, MidpointRounding.AwayFromZero),
            "EUR" => decimal.Round(rounded * rates.EurTry, 4, MidpointRounding.AwayFromZero),
            _ => throw new InvalidOperationException("Desteklenmeyen para birimi.")
        };
    }

    public static decimal Convert(decimal sourceAmount, string sourceCurrency, string destinationCurrency, FxRateOptions rates)
    {
        var source = Normalize(sourceCurrency);
        var destination = Normalize(destinationCurrency);
        if (source == destination)
            throw new InvalidOperationException("Aynı para birimi için virman kullanın.");

        var tryAmount = ToTry(sourceAmount, source, rates);
        return Normalize(destination) switch
        {
            "TRY" => tryAmount,
            "USD" => decimal.Round(tryAmount / rates.UsdTry, 4, MidpointRounding.AwayFromZero),
            "EUR" => decimal.Round(tryAmount / rates.EurTry, 4, MidpointRounding.AwayFromZero),
            _ => throw new InvalidOperationException("Desteklenmeyen para birimi.")
        };
    }
}
