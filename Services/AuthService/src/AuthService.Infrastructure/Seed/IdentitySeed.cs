namespace AuthService.Infrastructure.Seed;

public static class IdentitySeed
{
    public static readonly Guid CustomerRoleId = Guid.Parse("a1a1a1a1-0001-4000-8000-000000000001");
    public static readonly Guid OperationsRoleId = Guid.Parse("a1a1a1a1-0001-4000-8000-000000000002");

    public static readonly Guid KycReviewPermissionId = Guid.Parse("a1a1a1a1-0002-4000-8000-000000000001");
    public static readonly Guid CustomersReadPermissionId = Guid.Parse("a1a1a1a1-0002-4000-8000-000000000002");
    public static readonly Guid RolesAssignPermissionId = Guid.Parse("a1a1a1a1-0002-4000-8000-000000000003");

    public static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public const string CustomerRoleName = "Customer";
    public const string OperationsRoleName = "Operations";
    public const string KycReviewPermissionCode = "kyc:review";
    public const string CustomersReadPermissionCode = "customers:read";
    public const string RolesAssignPermissionCode = "roles:assign";
}
