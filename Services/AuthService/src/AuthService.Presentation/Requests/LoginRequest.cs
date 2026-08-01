namespace AuthService.Presentation.Requests.Authentication;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    // Mobil uygulama veya web tarafından üretilen cihaz kimliği
    public string DeviceId { get; set; } = string.Empty;

    // İsteğe bağlı
    public string? DeviceName { get; set; }

    public string? OperatingSystem { get; set; }

    public string? Browser { get; set; }
}