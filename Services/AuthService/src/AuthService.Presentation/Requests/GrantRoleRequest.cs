namespace AuthService.Presentation.Requests.Authentication;

public class GrantRoleRequest
{
    public Guid UserId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
