using FluentValidation;

namespace AuthService.Application.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("E-posta adresi zorunludur.")
            .EmailAddress()
            .WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256);

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Doğrulama kodu zorunludur.")
            .Length(6)
            .WithMessage("Doğrulama kodu 6 haneli olmalıdır.")
            .Matches(@"^\d{6}$")
            .WithMessage("Doğrulama kodu yalnızca rakam içermelidir.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Yeni şifre zorunludur.")
            .MinimumLength(8)
            .WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128);
    }
}
