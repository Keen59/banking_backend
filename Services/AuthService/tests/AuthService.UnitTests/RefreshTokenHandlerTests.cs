using AuthService.Application.Commands.RefreshToken;
using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using NSubstitute;
using Xunit;

namespace AuthService.UnitTests;

public class RefreshTokenHandlerTests
{
    [Fact]
    public async Task Handle_rotates_refresh_token_and_revokes_previous()
    {
        var user = AuthFakes.ActiveUser();
        var familyId = Guid.NewGuid();
        var current = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            Token = "current-hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedIp = "127.0.0.1"
        };
        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            Token = "next-hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedIp = "127.0.0.1"
        };
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenId = current.Id,
            JwtId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsActive = true
        };

        var users = Substitute.For<IUserRepository>();
        users.GetRolesWithPermissionsByIdAsync(user.Id, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(user);

        var refreshTokens = Substitute.For<IRefreshTokenRepository>();
        refreshTokens.GetByTokenAsync("current-hash", Arg.Any<CancellationToken>()).Returns(current);

        var sessions = Substitute.For<IUserSessionRepository>();
        sessions.GetActiveByRefreshTokenIdAsync(current.Id, Arg.Any<CancellationToken>()).Returns(session);

        var unitOfWork = AuthFakes.UnitOfWork(users, refreshTokens, sessions);
        var jwt = Substitute.For<IJwtService>();
        jwt.GenerateAccessToken(user, session, Arg.Any<IEnumerable<Permission>>())
            .Returns(AuthFakes.AccessToken());

        var refreshTokenService = Substitute.For<IRefreshTokenService>();
        refreshTokenService.Hash("old-plain").Returns("current-hash");
        refreshTokenService.Generate(user.Id, familyId)
            .Returns(new IssuedRefreshToken(replacement, "new-plain"));

        var handler = new RefreshTokenHandler(jwt, refreshTokenService, unitOfWork);

        var response = await handler.Handle(new RefreshTokenCommand
        {
            RefreshToken = "old-plain",
            IpAddress = "10.0.0.1"
        }, CancellationToken.None);

        Assert.Equal("new-plain", response.RefreshToken);
        Assert.NotNull(current.RevokedAt);
        Assert.Equal(replacement.Id, current.ReplacedByTokenId);
        Assert.Equal(replacement.Id, session.RefreshTokenId);
        refreshTokenService.Received(1).RememberRotation("current-hash", "new-plain");
        await refreshTokens.Received(1).AddAsync(replacement, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_reuse_of_revoked_token_outside_grace_throws_and_revokes_family()
    {
        var user = AuthFakes.ActiveUser();
        var familyId = Guid.NewGuid();
        var revoked = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            Token = "old-hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedIp = "127.0.0.1"
        };
        var sibling = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            Token = "sibling-hash",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedIp = "127.0.0.1"
        };
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshTokenId = sibling.Id,
            JwtId = Guid.NewGuid(),
            IpAddress = "127.0.0.1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsActive = true
        };

        var refreshTokens = Substitute.For<IRefreshTokenRepository>();
        refreshTokens.GetByTokenAsync("old-hash", Arg.Any<CancellationToken>()).Returns(revoked);
        refreshTokens.GetByFamilyIdAsync(familyId, Arg.Any<CancellationToken>())
            .Returns([revoked, sibling]);

        var sessions = Substitute.For<IUserSessionRepository>();
        sessions.GetActiveByRefreshTokenIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([session]);

        var auditLogs = Substitute.For<IAuditLogRepository>();
        var unitOfWork = AuthFakes.UnitOfWork(refreshTokens: refreshTokens, sessions: sessions, auditLogs: auditLogs);

        var refreshTokenService = Substitute.For<IRefreshTokenService>();
        refreshTokenService.Hash("old-plain").Returns("old-hash");
        refreshTokenService.TryGetRotatedPlaintext("old-hash").Returns((string?)null);

        var handler = new RefreshTokenHandler(
            Substitute.For<IJwtService>(),
            refreshTokenService,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RefreshTokenCommand
            {
                RefreshToken = "old-plain",
                IpAddress = "10.0.0.8"
            }, CancellationToken.None));

        Assert.Equal("Geçersiz veya süresi dolmuş token.", exception.Message);
        Assert.NotNull(sibling.RevokedAt);
        Assert.False(session.IsActive);
        await auditLogs.Received(1).AddAsync(
            Arg.Is<AuditLog>(log => log.Action == EAuditAction.RefreshTokenReuseDetected),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }
}
