namespace AuthService.Presentation.Requests.Authentication;

public class VerifyTwoFactorRequest
{
    public string Code { get; set; } = string.Empty;
}
