using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.Login
{
    public class LoginHandler(IPasswordHasherService _passwordHasher, IJwtService _jwtService, IRefreshTokenService _refreshTokenService, IUnitOfWork _unitOfWork) : IRequestHandler<LoginCommand, LoginResponse>
    {


        public async Task<LoginResponse> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.UserRepository.GetRolesWithPermissionsByEmailAsync(request.Email, cancellationToken: cancellationToken);

            if (user is null)
            {
                await AddFailedLoginAttempt(request, user?.Id, ELoginFailureReason.UserNotFound, cancellationToken);

                throw new UnauthorizedAccessException("Geçersiz kullanıcı adı veya şifre.");
            }

            if (user.LockoutEnd.HasValue &&
                user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                throw new UnauthorizedAccessException("Hesap geçici olarak kilitlenmiştir.");
            }

            var passwordValid = _passwordHasher.Verify(
                request.Password,
                user.PasswordHash);

            if (!passwordValid)
            {
                user.FailedLoginCount++;

                if (user.FailedLoginCount >= 5)
                {
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
                }

                _unitOfWork.UserRepository.Update(user);

                await AddFailedLoginAttempt(request, user.Id, ELoginFailureReason.InvalidPassword, cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);

                throw new UnauthorizedAccessException("Geçersiz kullanıcı adı veya şifre.");
            }

            if (user.Status != EUserStatus.Active)
            {
                throw new UnauthorizedAccessException("Kullanıcı aktif değil.");
            }

            if (!user.IsEmailVerified)
            {
                throw new UnauthorizedAccessException("Email doğrulanmamış.");
            }

            user.FailedLoginCount = 0;
            user.LockoutEnd = null;

            var device = await _unitOfWork.DeviceRepository.GetByIdentifierAsync(request.DeviceId);

            if (device is null)
            {
                device = new Device
                {
                    UserId = user.Id,
                    DeviceIdentifier = request.DeviceId,
                    DeviceName = request.DeviceName ?? "",
                    Browser = request.Browser ?? "",
                    OperatingSystem = request.OperatingSystem ?? "",
                    IpAddress = request.IpAddress,
                    IsTrusted = false
                };

                await _unitOfWork.DeviceRepository.AddAsync(device, cancellationToken);
            }

            var refreshToken = await _refreshTokenService.Generate(user.Id);

            await _unitOfWork.RefreshTokenRepository.AddAsync(refreshToken, cancellationToken);

            var session = new UserSession
            {
                UserId = user.Id,
                RefreshTokenId = refreshToken.Id,
                DeviceId = device.Id,
                IpAddress = request.IpAddress,
                ExpiresAt = refreshToken.ExpiresAt,
                LastActivityAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            await _unitOfWork.UserSessionRepository.AddAsync(session, cancellationToken);

            var permissions = user.UserRoles
                                    .SelectMany(x => x.Role.RolePermissions)
                                    .Select(x => x.Permission)
                                    .DistinctBy(x => x.Id)
                                    .ToList();

            var accessToken = await _jwtService.GenerateAccessToken(user, session, permissions);

            await _unitOfWork.AuditLogRepository.AddAsync(new AuditLog
            {
                UserId = user.Id,
                Action = EAuditAction.Login,
                Resource = "Authentication",
                IpAddress = request.IpAddress,
                Device = request.DeviceName ?? ""
            }, cancellationToken);

            await _unitOfWork.LoginAttemptRepository.AddAsync(new LoginAttempt
            {
                UserId = user.Id,
                Email = request.Email,
                IpAddress = request.IpAddress,
                Device = request.DeviceName ?? "",
                IsSuccessful = true
            }, cancellationToken);

            _unitOfWork.UserRepository.Update(user);

            await _unitOfWork.SaveAsync(cancellationToken);

            return new LoginResponse
            {
                Message = "Giriş başarılı.",
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken.Token,
                AccessTokenExpiresAt = accessToken.ExpiresAt,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    CustomerId = user.CustomerId,
                    Email = user.Email,
                    Username = user.Username,
                    Roles = user.UserRoles
                        .Select(x => x.Role.Name)
                        .ToList()
                }
            };
        }

        private async Task AddFailedLoginAttempt(
                        LoginCommand request,
                        Guid? userId,
                        ELoginFailureReason reason,
                        CancellationToken cancellationToken)
        {
            var loginAttempt = new LoginAttempt
            {
                UserId = userId,
                Email = request.Email,
                IpAddress = request.IpAddress,
                Device = request.DeviceName ?? request.DeviceId,
                IsSuccessful = false,
                FailureReason = reason,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _unitOfWork.LoginAttemptRepository.AddAsync(loginAttempt, cancellationToken: cancellationToken);
        }
    }


}
