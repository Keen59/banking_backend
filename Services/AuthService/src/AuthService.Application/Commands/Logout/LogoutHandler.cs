using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AuthService.Application.Commands.Logout;

public class LogoutHandler(IUnitOfWork _unitOfWork) : IRequestHandler<LogoutCommand, LogoutResponse>
{
    public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            return new LogoutResponse
            {
                IsSuccess = false,
                Message = "Geçersiz SessionId."
            };
        }

        UserSession? session = await _unitOfWork.UserSessionRepository.GetSessionWithRefreshTokenById(sessionId);

        if (session==null)
            return new LogoutResponse
            {
                IsSuccess = false,
                Message = "Session"
            };

        if (!session.IsActive)
        {
            return new LogoutResponse
            {

                IsSuccess = false,
                Message = "Session zaten sonlandırılmış."
            };
        }

        session.IsActive = false;
        session.RevokedAt = DateTimeOffset.UtcNow;
        session.LastActivityAt = DateTimeOffset.UtcNow;

        session.RefreshToken.RevokedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.AuditLogRepository.AddAsync(
                new AuditLog
                {
                    UserId = session.UserId,
                    Action = EAuditAction.Logout,
                    Resource = "Authentication"
                },
                cancellationToken);

        await _unitOfWork.SaveAsync(cancellationToken);

        return new LogoutResponse
        {
        };
    }
}
