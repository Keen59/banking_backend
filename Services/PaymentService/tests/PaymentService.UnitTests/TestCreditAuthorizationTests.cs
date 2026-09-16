using System.Reflection;
using Banking.Contracts.Authorization;
using Microsoft.AspNetCore.Authorization;
using PaymentService.Presentation.Controllers;
using Xunit;

namespace PaymentService.UnitTests;

public class TestCreditAuthorizationTests
{
    [Fact]
    public void Create_requires_payments_credit_policy()
    {
        var method = typeof(TestCreditsController).GetMethod(nameof(TestCreditsController.Create));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.PaymentsCredit, authorize!.Policy);
        Assert.Equal("payments:credit", Permissions.PaymentsCredit);
    }
}
