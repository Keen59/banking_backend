namespace AuthService.Application.DTOs;

public class Response
{
    public bool IsSuccess { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}
