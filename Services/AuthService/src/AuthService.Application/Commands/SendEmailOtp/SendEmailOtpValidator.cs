using AuthService.Domain.Enums;
using FluentValidation;

namespace AuthService.Application.Commands.SendEmailOtp;

public class SendEmailOtpCommandValidator : AbstractValidator<SendEmailOtpCommand>
{
    public SendEmailOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("E-posta adresi zorunludur.")
            .EmailAddress()
            .WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256);

        RuleFor(x => x.Purpose)
            .Must(purpose => purpose is EOtpPurpose.EmailVerification or EOtpPurpose.PasswordReset)
            .WithMessage("Geçerli bir OTP amacı seçiniz.");
    }
}
