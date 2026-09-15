namespace Banking.Contracts.Authorization;

public static class Roles
{
    public const string Customer = "Customer";
    public const string Operations = "Operations";
}

public static class Permissions
{
    public const string KycReview = "kyc:review";
    public const string CustomersRead = "customers:read";
    public const string RolesAssign = "roles:assign";
}

public static class AuthorizationPolicies
{
    public const string KycReview = "KycReview";
    public const string CustomersRead = "CustomersRead";
}
