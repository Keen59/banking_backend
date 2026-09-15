using AuthService.Application.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace AuthService.Infrastructure.Services;

public class EmailOtpService(
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    IConfiguration configuration) : IEmailOtpService
{
    private const int ExpiryMinutes = 5;
    private const int MaxAttempts = 5;
    private const int CooldownSeconds = 60;
    private const int CodeLength = 6;

    public async Task<bool> IssueAsync(User user, EOtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var latest = await unitOfWork.EmailOtpRepository.GetLatestAsync(user.Id, purpose, cancellationToken);
        if (latest is not null &&
            latest.ConsumedAt is null &&
            DateTimeOffset.UtcNow - latest.CreatedAt < TimeSpan.FromSeconds(CooldownSeconds))
        {
            return false;
        }

        var previous = await unitOfWork.EmailOtpRepository.GetUnconsumedAsync(user.Id, purpose, cancellationToken);
        foreach (var otp in previous)
        {
            otp.ConsumedAt = DateTimeOffset.UtcNow;
        }

        var code = GenerateTokenHelper.GenerateNumericOtp(CodeLength);
        await unitOfWork.EmailOtpRepository.AddAsync(new EmailOtp
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email,
            Purpose = purpose,
            CodeHash = HashCode(user.Id, purpose, code),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(ExpiryMinutes)
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        var (subject, body) = BuildMessage(purpose, code);
        await emailSender.SendAsync(user.Email, subject, body, cancellationToken);
        return true;
    }

    public async Task<User> VerifyAndConsumeAsync(
        string email,
        string code,
        EOtpPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var user = await unitOfWork.UserRepository.GetByEmailAsync(email, cancellationToken: cancellationToken);
        if (user is null)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş kod.");

        var otp = await unitOfWork.EmailOtpRepository.GetActiveAsync(user.Id, purpose, cancellationToken);
        if (otp is null || otp.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş kod.");

        if (otp.FailedAttemptCount >= MaxAttempts)
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş kod.");

        if (!CryptographicEquals(otp.CodeHash, HashCode(user.Id, purpose, code.Trim())))
        {
            otp.FailedAttemptCount++;
            if (otp.FailedAttemptCount >= MaxAttempts)
            {
                otp.ConsumedAt = DateTimeOffset.UtcNow;
            }

            await unitOfWork.SaveAsync(cancellationToken);
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş kod.");
        }

        otp.ConsumedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveAsync(cancellationToken);

        return user;
    }

    private string HashCode(Guid userId, EOtpPurpose purpose, string code)
    {
        var secret = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey yapılandırılmamış.");

        return GenerateTokenHelper.ComputeHmacSha256(secret, $"{userId:N}:{(int)purpose}:{code}");
    }

    private static bool CryptographicEquals(string left, string right)
    {
        var leftBytes = System.Text.Encoding.UTF8.GetBytes(left);
        var rightBytes = System.Text.Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length &&
               System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static (string Subject, string Body) BuildMessage(EOtpPurpose purpose, string code)
    {
        var reason = purpose switch
        {
            EOtpPurpose.EmailVerification => "e-posta doğrulama",
            EOtpPurpose.PasswordReset => "şifre sıfırlama",
            EOtpPurpose.Login => "giriş doğrulama",
            EOtpPurpose.TwoFactorSetup => "iki faktörlü doğrulama kurulumu",
            EOtpPurpose.TwoFactorDisable => "iki faktörlü doğrulamayı kapatma",
            _ => "doğrulama"
        };

        return (
            $"Doğrulama kodunuz ({reason})",
            $"Doğrulama kodunuz: {code}. Kod {ExpiryMinutes} dakika geçerlidir. Bu kodu kimseyle paylaşmayın.");
    }
}
