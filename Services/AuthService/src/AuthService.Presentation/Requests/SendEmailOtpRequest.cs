using System.Text.Json.Serialization;
using AuthService.Domain.Enums;

namespace AuthService.Presentation.Requests.Authentication;

public class SendEmailOtpRequest
{
    public string Email { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EOtpPurpose Purpose { get; set; }
}
