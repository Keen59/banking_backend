using System.Reflection;
using Banking.Contracts.Authorization;
using Microsoft.AspNetCore.Authorization;
using PaymentService.Presentation.Controllers;
using Xunit;

namespace PaymentService.UnitTests;

public class IncomingEftPaymentAuthorizationTests
{
    [Fact]
    public void Create_requires_payments_credit_policy()
    {
        var method = typeof(IncomingEftPaymentsController).GetMethod(nameof(IncomingEftPaymentsController.Create));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.PaymentsCredit, authorize!.Policy);
        Assert.Equal("payments:credit", Permissions.PaymentsCredit);
    }

    [Fact]
    public void Settle_requires_payments_settle_policy()
    {
        var method = typeof(IncomingEftPaymentsController).GetMethod(nameof(IncomingEftPaymentsController.Settle));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.PaymentsSettle, authorize!.Policy);
    }

    [Fact]
    public void Reject_requires_payments_settle_policy()
    {
        var method = typeof(IncomingEftPaymentsController).GetMethod(nameof(IncomingEftPaymentsController.Reject));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.PaymentsSettle, authorize!.Policy);
    }
}
