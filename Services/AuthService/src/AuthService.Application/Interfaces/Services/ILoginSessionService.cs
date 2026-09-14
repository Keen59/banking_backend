using AuthService.Application.DTOs.Authentication;
using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Services;

public interface ILoginSessionService
{
    Task<LoginResponse> IssueSessionAsync(User user, LoginContext context, CancellationToken cancellationToken = default);
}
