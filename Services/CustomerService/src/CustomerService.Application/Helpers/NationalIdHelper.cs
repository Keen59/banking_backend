namespace CustomerService.Application.Helpers;

public static class NationalIdHelper
{
    public static bool IsValidTckn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 11 || value[0] == '0' || !value.All(char.IsDigit))
            return false;

        var digits = value.Select(c => c - '0').ToArray();
        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        var digit10 = ((oddSum * 7) - evenSum) % 10;
        if (digit10 < 0)
            digit10 += 10;

        if (digits[9] != digit10)
            return false;

        return digits[10] == digits.Take(10).Sum() % 10;
    }
}
