namespace AuthService.Presentation.Requests.Authentication;

public class VerifyEmailOtpRequest
{
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}
