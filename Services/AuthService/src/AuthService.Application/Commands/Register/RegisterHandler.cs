using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using Banking.Contracts.Authorization;
using Banking.Contracts.Events;
using MediatR;

namespace AuthService.Application.Commands.Register;

public class RegisterHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasherService passwordHasher,
    IEmailOtpService emailOtpService,
    IIntegrationEventPublisher eventPublisher) : IRequestHandler<RegisterCommand, RegisterResponse>
{
    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim();

        if (await unitOfWork.UserRepository.GetByEmailAsync(email, cancellationToken: cancellationToken) is not null)
            throw new InvalidOperationException("Bu e-posta adresi zaten kayıtlı.");

        if (await unitOfWork.UserRepository.GetByUsernameAsync(username, cancellationToken) is not null)
            throw new InvalidOperationException("Bu kullanıcı adı zaten alınmış.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Email = email,
            Username = username,
            PhoneNumber = request.PhoneNumber.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            Status = EUserStatus.Active,
            IsEmailVerified = false,
            IsPhoneVerified = false,
            IsTwoFactorEnabled = false
        };

        await unitOfWork.UserRepository.AddAsync(user, cancellationToken);

        var customerRole = await unitOfWork.RoleRepository.GetByNameAsync(Roles.Customer, cancellationToken)
            ?? throw new InvalidOperationException("Customer rolü bulunamadı. Veritabanı migration uygulayın.");

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = customerRole.Id
        });

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = EAuditAction.UserRegistered,
            Resource = "Authentication"
        }, cancellationToken);

        await eventPublisher.PublishAsync(new UserRegistered(
            user.Id,
            user.CustomerId,
            user.Email,
            user.Username,
            user.PhoneNumber,
            DateTimeOffset.UtcNow), cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        await emailOtpService.IssueAsync(user, EOtpPurpose.EmailVerification, cancellationToken);

        return new RegisterResponse
        {
            UserId = user.Id,
            Message = "Kayıt başarılı. E-posta adresinize gönderilen kod ile hesabınızı doğrulayın."
        };
    }
}
