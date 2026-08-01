using AuthService.Application.Interfaces.Services;

namespace AuthService.Infrastructure.Services
{
    public class PaswordHasherService : IPasswordHasherService
    {
        public bool Verify(string password, string passwordHash)
        {
            throw new NotImplementedException();
        }
    }
}
