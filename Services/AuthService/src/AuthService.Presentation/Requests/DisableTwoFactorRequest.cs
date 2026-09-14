namespace AuthService.Presentation.Requests.Authentication;

public class DisableTwoFactorRequest
{
    public string Password { get; set; } = string.Empty;
}
