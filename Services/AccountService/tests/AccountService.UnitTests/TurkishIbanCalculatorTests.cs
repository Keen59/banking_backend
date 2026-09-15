using System.Text;
using AccountService.Application.Helpers;
using Xunit;

namespace AccountService.UnitTests;

public class TurkishIbanCalculatorTests
{
    [Fact]
    public void Build_returns_26_character_iban_with_valid_mod97_check_digits()
    {
        var iban = TurkishIbanCalculator.Build("00100", "1234567890123456");

        Assert.Equal(26, iban.Length);
        Assert.StartsWith("TR", iban, StringComparison.Ordinal);
        Assert.Equal("00100", iban[4..9]);
        Assert.Equal("0", iban[9].ToString());
        Assert.Equal("1234567890123456", iban[10..]);
        Assert.True(IsValidIso7064Mod97(iban));
    }

    [Theory]
    [InlineData("0100", "1234567890123456")]
    [InlineData("0010A", "1234567890123456")]
    [InlineData("00100", "123456789012345")]
    [InlineData("00100", "123456789012345A")]
    public void Build_rejects_invalid_bank_code_or_account_number(string bankCode, string accountNumber)
    {
        Assert.Throws<InvalidOperationException>(() => TurkishIbanCalculator.Build(bankCode, accountNumber));
    }

    [Fact]
    public void Build_is_deterministic_for_the_same_bank_and_account()
    {
        var first = TurkishIbanCalculator.Build("00100", "0000000000000001");
        var second = TurkishIbanCalculator.Build("00100", "0000000000000001");
        Assert.Equal(first, second);
    }

    private static bool IsValidIso7064Mod97(string iban)
    {
        var rearranged = iban[4..] + iban[..4];
        var numeric = new StringBuilder(rearranged.Length * 2);
        foreach (var character in rearranged)
        {
            if (char.IsDigit(character))
                numeric.Append(character);
            else
                numeric.Append(character - 'A' + 10);
        }

        var remainder = 0;
        foreach (var character in numeric.ToString())
            remainder = (remainder * 10 + (character - '0')) % 97;

        return remainder == 1;
    }
}
