using System.Text;

namespace PaymentService.Application.Helpers;

public static class TurkishIban
{
    public static string Normalize(string? iban)
    {
        return (iban ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
    }

    public static string Build(string bankCode, string accountNumber)
    {
        if (bankCode.Length != 5 || !bankCode.All(char.IsDigit))
            throw new InvalidOperationException("Banka kodu 5 haneli rakam olmalıdır.");

        if (accountNumber.Length != 16 || !accountNumber.All(char.IsDigit))
            throw new InvalidOperationException("Hesap numarası 16 haneli rakam olmalıdır.");

        var bban = bankCode + "0" + accountNumber;
        var remainder = Mod97(ToNumeric(bban + "TR00"));
        return "TR" + (98 - remainder).ToString("D2") + bban;
    }

    public static bool IsValid(string iban)
    {
        if (iban.Length != 26)
            return false;

        if (!iban.StartsWith("TR", StringComparison.Ordinal))
            return false;

        if (!iban.Skip(2).All(char.IsLetterOrDigit))
            return false;

        return Mod97(ToNumeric(iban[4..] + iban[..4])) == 1;
    }

    private static int Mod97(string numeric)
    {
        var remainder = 0;
        foreach (var character in numeric)
            remainder = (remainder * 10 + (character - '0')) % 97;
        return remainder;
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
