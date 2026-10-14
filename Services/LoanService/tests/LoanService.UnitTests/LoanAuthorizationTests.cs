using System.Reflection;
using Banking.Contracts.Authorization;
using Microsoft.AspNetCore.Authorization;
using LoanService.Presentation.Controllers;
using Xunit;

namespace LoanService.UnitTests;

public class LoanAuthorizationTests
{
    [Fact]
    public void Approve_requires_loans_review_policy()
    {
        var method = typeof(LoansController).GetMethod(nameof(LoansController.Approve));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.LoansReview, authorize!.Policy);
        Assert.Equal("loans:review", Permissions.LoansReview);
    }

    [Fact]
    public void Reject_requires_loans_review_policy()
    {
        var method = typeof(LoansController).GetMethod(nameof(LoansController.Reject));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.LoansReview, authorize!.Policy);
        Assert.Equal("loans:review", Permissions.LoansReview);
    }
}
