using AuthService.Application.Commands.Logout;
using AuthService.Application.Helpers;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using MediatR;

namespace AuthService.Application.Commands.ForgotPassword;

public class ForgotPasswordHandler(IUnitOfWork _unitOfWork) : IRequestHandler<ForgosPasswordCommand, ForgotPasswordResponse>
{
    public async Task<ForgotPasswordResponse> Handle(ForgosPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.UserRepository.GetByEmailAsync(request.Email);
        if (user == null)
            new ForgotPasswordResponse
            {
                IsSuccess = false,
                Message = "Kullanıcı bulunamadı."
            };

        var resetToken = GenerateTokenHelper.GenerateResetToken();
        var hashedToken = GenerateTokenHelper.ComputeSha256(resetToken);
        await _unitOfWork.PasswordResetTokenRepository.AddAsync(
             new PasswordResetToken
             {
                 UserId = user.Id,
                 TokenHash = hashedToken,
                 ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
                 UsedAt = null
             }, cancellationToken);
        return new ForgotPasswordResponse
        {
            IsSuccess = true,
            Message = "Şifre sıfırlama talimatları e-posta adresinize gönderildi.",
            ResetToken = resetToken
        };
    }
}
