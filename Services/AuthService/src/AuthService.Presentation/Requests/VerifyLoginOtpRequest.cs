namespace AuthService.Presentation.Requests.Authentication;

public class VerifyLoginOtpRequest
{
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string? DeviceName { get; set; }

    public string? OperatingSystem { get; set; }

    public string? Browser { get; set; }
}
