namespace AuthService.Domain.Entities;

public class Permission : BaseEntity
{
    public string Code { get; set; } = null!;

    public string Description { get; set; } = null!;

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
