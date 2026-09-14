using AuthService.Domain.Enums;
using FluentValidation;

namespace AuthService.Application.Commands.VerifyEmailOtp;

public class VerifyEmailOtpCommandValidator : AbstractValidator<VerifyEmailOtpCommand>
{
    public VerifyEmailOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("E-posta adresi zorunludur.")
            .EmailAddress()
            .WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256);

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Doğrulama kodu zorunludur.")
            .Length(6)
            .WithMessage("Doğrulama kodu 6 haneli olmalıdır.")
            .Matches(@"^\d{6}$")
            .WithMessage("Doğrulama kodu yalnızca rakam içermelidir.");

        RuleFor(x => x.Purpose)
            .Equal(EOtpPurpose.EmailVerification)
            .WithMessage("Bu uç yalnızca e-posta doğrulama için kullanılır.");
    }
}
