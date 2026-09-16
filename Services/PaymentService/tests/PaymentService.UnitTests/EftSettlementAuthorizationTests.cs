using System.Reflection;
using Banking.Contracts.Authorization;
using Microsoft.AspNetCore.Authorization;
using PaymentService.Presentation.Controllers;
using Xunit;

namespace PaymentService.UnitTests;

public class EftSettlementAuthorizationTests
{
    [Fact]
    public void Settle_requires_payments_settle_policy()
    {
        var method = typeof(EftPaymentsController).GetMethod(nameof(EftPaymentsController.Settle));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.PaymentsSettle, authorize!.Policy);
        Assert.Equal("payments:settle", Permissions.PaymentsSettle);
    }

    [Fact]
    public void Reject_requires_payments_settle_policy()
    {
        var method = typeof(EftPaymentsController).GetMethod(nameof(EftPaymentsController.Reject));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.PaymentsSettle, authorize!.Policy);
    }
}
