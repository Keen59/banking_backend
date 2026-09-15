using FluentValidation;

namespace AuthService.Application.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("E-posta adresi zorunludur.")
            .EmailAddress()
            .WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256);

        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Kullanıcı adı zorunludur.")
            .MinimumLength(3)
            .WithMessage("Kullanıcı adı en az 3 karakter olmalıdır.")
            .MaximumLength(50)
            .Matches(@"^[a-zA-Z0-9._-]+$")
            .WithMessage("Kullanıcı adı yalnızca harf, rakam, nokta, alt çizgi ve tire içerebilir.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Şifre zorunludur.")
            .MinimumLength(8)
            .WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Telefon numarası zorunludur.")
            .MaximumLength(20);
    }
}
