using System.Reflection;
using Banking.Contracts.Authorization;
using Microsoft.AspNetCore.Authorization;
using RiskService.Presentation.Controllers;
using Xunit;

namespace RiskService.UnitTests;

public class RiskAlertAuthorizationTests
{
    [Fact]
    public void GetOpen_requires_risk_review_policy()
    {
        var method = typeof(RiskAlertsController).GetMethod(nameof(RiskAlertsController.GetOpen));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.RiskReview, authorize!.Policy);
        Assert.Equal("risk:review", Permissions.RiskReview);
    }

    [Fact]
    public void GetById_requires_risk_review_policy()
    {
        var method = typeof(RiskAlertsController).GetMethod(nameof(RiskAlertsController.GetById));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.RiskReview, authorize!.Policy);
    }

    [Fact]
    public void Close_requires_risk_review_policy()
    {
        var method = typeof(RiskAlertsController).GetMethod(nameof(RiskAlertsController.Close));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.RiskReview, authorize!.Policy);
    }
}
