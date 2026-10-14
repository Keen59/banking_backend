namespace LoanService.Application.Helpers;

public static class LoanInstallmentCalculator
{
    public static decimal MonthlyAmount(decimal amount, int termMonths)
        => Math.Round(amount / termMonths, 2, MidpointRounding.AwayFromZero);

    public static decimal AmountForSequence(decimal amount, int termMonths, int sequence)
    {
        var monthly = MonthlyAmount(amount, termMonths);
        if (sequence < termMonths)
            return monthly;

        return amount - monthly * (termMonths - 1);
    }
}
