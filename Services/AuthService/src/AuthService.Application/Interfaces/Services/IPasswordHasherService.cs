namespace AuthService.Application.Interfaces.Services;

public interface IPasswordHasherService
{
    bool Verify(string password, string passwordHash);
}