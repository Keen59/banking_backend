using PaymentService.Application.Helpers;
using Xunit;

namespace PaymentService.UnitTests;

public class TurkishIbanTests
{
    [Fact]
    public void Build_is_valid_iso7064_mod97()
    {
        var iban = TurkishIban.Build("00012", "9999999999999999");
        Assert.Equal(26, iban.Length);
        Assert.True(TurkishIban.IsValid(iban));
    }

    [Fact]
    public void IsValid_rejects_wrong_check_digits()
    {
        Assert.False(TurkishIban.IsValid("TR000000000000000000000000"));
    }
}
