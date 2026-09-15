using System.Reflection;
using Banking.Contracts.Authorization;
using CustomerService.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace CustomerService.UnitTests;

public class ReviewKycAuthorizationTests
{
    [Fact]
    public void ReviewKyc_requires_kyc_review_policy()
    {
        var method = typeof(CustomersController).GetMethod(nameof(CustomersController.ReviewKyc));
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.KycReview, authorize!.Policy);
        Assert.Equal("kyc:review", Permissions.KycReview);
    }
}
