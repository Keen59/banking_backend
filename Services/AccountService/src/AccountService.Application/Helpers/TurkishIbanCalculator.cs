using System.Text;

namespace AccountService.Application.Helpers;

public static class TurkishIbanCalculator
{
    public static string Build(string bankCode, string accountNumber)
    {
        if (bankCode.Length != 5 || !bankCode.All(char.IsDigit))
            throw new InvalidOperationException("Banka kodu 5 haneli rakam olmalıdır.");

        if (accountNumber.Length != 16 || !accountNumber.All(char.IsDigit))
            throw new InvalidOperationException("Hesap numarası 16 haneli rakam olmalıdır.");

        var bban = bankCode + "0" + accountNumber;
        var checkDigits = ComputeCheckDigits(bban);
        return "TR" + checkDigits + bban;
    }

    private static string ComputeCheckDigits(string bban)
    {
        var rearranged = ToNumeric(bban + "TR00");
        var remainder = 0;
        foreach (var character in rearranged)
            remainder = (remainder * 10 + (character - '0')) % 97;

        return (98 - remainder).ToString("D2");
    }

    private static string ToNumeric(string value)
    {
        var builder = new StringBuilder(value.Length * 2);
        foreach (var character in value)
        {
            if (char.IsDigit(character))
                builder.Append(character);
            else
                builder.Append(character - 'A' + 10);
        }

        return builder.ToString();
    }
}
