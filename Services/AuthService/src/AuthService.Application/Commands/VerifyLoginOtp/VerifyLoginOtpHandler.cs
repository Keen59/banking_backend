using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.VerifyLoginOtp;

public class VerifyLoginOtpHandler(
    IEmailOtpService emailOtpService,
    ILoginSessionService loginSessionService,
    IUnitOfWork unitOfWork) : IRequestHandler<VerifyLoginOtpCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(VerifyLoginOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByEmailAsync(request.Email, cancellationToken: cancellationToken);

        if (user is null ||
            !user.IsTwoFactorEnabled ||
            user.Status != EUserStatus.Active ||
            !user.IsEmailVerified)
        {
            await AddFailedOtpAttempt(request, user?.Id, cancellationToken);
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş kod.");
        }

        try
        {
            await emailOtpService.VerifyAndConsumeAsync(
                request.Email,
                request.Code,
                EOtpPurpose.Login,
                cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            await AddFailedOtpAttempt(request, user.Id, cancellationToken);
            throw;
        }

        return await loginSessionService.IssueSessionAsync(user, new LoginContext
        {
            Email = request.Email,
            DeviceId = request.DeviceId,
            DeviceName = request.DeviceName,
            Browser = request.Browser,
            OperatingSystem = request.OperatingSystem,
            IpAddress = request.IpAddress
        }, cancellationToken);
    }

    private async Task AddFailedOtpAttempt(
        VerifyLoginOtpCommand request,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        await unitOfWork.LoginAttemptRepository.AddAsync(new LoginAttempt
        {
            UserId = userId,
            Email = request.Email,
            IpAddress = request.IpAddress,
            Device = request.DeviceName ?? request.DeviceId,
            IsSuccessful = false,
            FailureReason = ELoginFailureReason.InvalidOtp,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);
    }
}
