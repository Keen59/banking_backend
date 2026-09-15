using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.RefreshToken;

public class RefreshTokenHandler(
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<RefreshTokenResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenService.Hash(request.RefreshToken);
        var currentToken = await unitOfWork.RefreshTokenRepository.GetByTokenAsync(
            tokenHash,
            cancellationToken);

        if (currentToken is null)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş token.");

        if (currentToken.IsRevoked)
        {
            var gracePlaintext = refreshTokenService.TryGetRotatedPlaintext(tokenHash);
            if (gracePlaintext is not null)
            {
                return await CompleteGraceRefresh(currentToken, gracePlaintext, request.IpAddress, cancellationToken);
            }

            await RevokeFamilyAndSessions(currentToken.FamilyId, request.IpAddress, cancellationToken);

            await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
            {
                UserId = currentToken.UserId,
                Action = EAuditAction.RefreshTokenReuseDetected,
                Resource = "Authentication",
                IpAddress = request.IpAddress
            }, cancellationToken);

            await unitOfWork.SaveAsync(cancellationToken);

            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş token.");
        }

        if (currentToken.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş token.");

        var session = await unitOfWork.UserSessionRepository.GetActiveByRefreshTokenIdAsync(
            currentToken.Id,
            cancellationToken);

        if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş token.");

        var user = await GetActiveUser(currentToken.UserId, cancellationToken);

        var issuedRefreshToken = await refreshTokenService.Generate(user.Id, currentToken.FamilyId);
        issuedRefreshToken.Entity.CreatedIp = request.IpAddress;
        issuedRefreshToken.Entity.DeviceId = currentToken.DeviceId;

        currentToken.RevokedAt = DateTimeOffset.UtcNow;
        currentToken.RevokedIp = request.IpAddress;
        currentToken.ReplacedByTokenId = issuedRefreshToken.Entity.Id;

        session.RefreshTokenId = issuedRefreshToken.Entity.Id;
        session.ExpiresAt = issuedRefreshToken.Entity.ExpiresAt;
        session.LastActivityAt = DateTimeOffset.UtcNow;
        session.IpAddress = request.IpAddress;
        session.JwtId = Guid.NewGuid();

        await unitOfWork.RefreshTokenRepository.AddAsync(issuedRefreshToken.Entity, cancellationToken);

        var accessToken = await GenerateAccessToken(user, session);

        refreshTokenService.RememberRotation(tokenHash, issuedRefreshToken.Plaintext);

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = EAuditAction.TokenRefreshed,
            Resource = "Authentication",
            IpAddress = request.IpAddress
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return MapResponse(accessToken, issuedRefreshToken.Plaintext, issuedRefreshToken.Entity.ExpiresAt, user);
    }

    private async Task<RefreshTokenResponse> CompleteGraceRefresh(
        Domain.Entities.RefreshToken currentToken,
        string rotatedPlaintext,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        if (!currentToken.ReplacedByTokenId.HasValue)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş token.");

        var replacement = await unitOfWork.RefreshTokenRepository.GetByIdAsync(currentToken.ReplacedByTokenId.Value);
        if (replacement is null || replacement.IsRevoked || replacement.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş token.");

        var session = await unitOfWork.UserSessionRepository.GetActiveByRefreshTokenIdAsync(
            replacement.Id,
            cancellationToken) ?? throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş token.");

        var user = await GetActiveUser(currentToken.UserId, cancellationToken);

        session.LastActivityAt = DateTimeOffset.UtcNow;
        session.IpAddress = ipAddress;
        session.JwtId = Guid.NewGuid();

        var accessToken = await GenerateAccessToken(user, session);
        await unitOfWork.SaveAsync(cancellationToken);

        return MapResponse(accessToken, rotatedPlaintext, replacement.ExpiresAt, user);
    }

    private async Task RevokeFamilyAndSessions(
        Guid familyId,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var familyTokens = await unitOfWork.RefreshTokenRepository.GetByFamilyIdAsync(familyId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var token in familyTokens.Where(token => !token.IsRevoked))
        {
            token.RevokedAt = now;
            token.RevokedIp = ipAddress;
        }

        var sessions = await unitOfWork.UserSessionRepository.GetActiveByRefreshTokenIdsAsync(
            familyTokens.Select(token => token.Id).ToList(),
            cancellationToken);

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.RevokedAt = now;
            session.LastActivityAt = now;
        }
    }

    private async Task<User> GetActiveUser(Guid userId, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetRolesWithPermissionsByIdAsync(
            userId,
            cancellationToken: cancellationToken);

        if (user is null ||
            user.Status != EUserStatus.Active ||
            !user.IsEmailVerified ||
            (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow))
        {
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş token.");
        }

        return user;
    }

    private async Task<AccessTokenDto> GenerateAccessToken(User user, UserSession session)
    {
        var permissions = user.UserRoles
            .SelectMany(x => x.Role.RolePermissions)
            .Select(x => x.Permission)
            .DistinctBy(x => x.Id)
            .ToList();

        return await jwtService.GenerateAccessToken(user, session, permissions)
            ?? throw new InvalidOperationException("Access token üretilemedi.");
    }

    private static RefreshTokenResponse MapResponse(
        AccessTokenDto accessToken,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        User user)
    {
        return new RefreshTokenResponse
        {
            Message = "Token yenilendi.",
            AccessToken = accessToken.Token,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            User = new UserInfoDto
            {
                Id = user.Id,
                CustomerId = user.CustomerId,
                Email = user.Email,
                Username = user.Username,
                Roles = user.UserRoles
                    .Select(x => x.Role.Name)
                    .ToList(),
                IsEmailVerified = user.IsEmailVerified,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled
            }
        };
    }
}
