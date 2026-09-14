using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

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

        if (session is null)
        {
            return new LogoutResponse
            {
                IsSuccess = false,
                Message = "Oturum bulunamadı."
            };
        }

        if (!string.IsNullOrWhiteSpace(request.UserId) &&
            Guid.TryParse(request.UserId, out var userId) &&
            session.UserId != userId)
        {
            return new LogoutResponse
            {
                IsSuccess = false,
                Message = "Oturum bu kullanıcıya ait değil."
            };
        }

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

        if (session.RefreshToken is not null)
        {
            session.RefreshToken.RevokedAt = DateTimeOffset.UtcNow;
        }

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
            IsSuccess = true,
            Message = "Çıkış başarılı."
        };
    }
}
