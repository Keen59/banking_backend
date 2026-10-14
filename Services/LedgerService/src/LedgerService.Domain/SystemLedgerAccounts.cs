namespace LedgerService.Domain;

public static class SystemLedgerAccounts
{
    public static readonly Guid InternalClearingTry = Guid.Parse("b1b1b1b1-0001-4000-8000-000000000001");

    public static readonly Guid InternalClearingUsd = Guid.Parse("b1b1b1b1-0001-4000-8000-000000000002");

    public static readonly Guid InternalClearingEur = Guid.Parse("b1b1b1b1-0001-4000-8000-000000000003");

    public static Guid ClearingFor(string currency) => currency.Trim().ToUpperInvariant() switch
    {
        "TRY" => InternalClearingTry,
        "USD" => InternalClearingUsd,
        "EUR" => InternalClearingEur,
        _ => throw new InvalidOperationException("Desteklenmeyen para birimi.")
    };
}
