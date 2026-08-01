using AuthService.Application.Features.Authentication.Commands.Login;
using FluentValidation;

namespace AuthService.Application.Features.Authentication.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("E-posta adresi zorunludur.")
            .EmailAddress()
            .WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Şifre zorunludur.")
            .MinimumLength(8)
            .WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128);

        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithMessage("Cihaz bilgisi zorunludur.")
            .MaximumLength(256);

        RuleFor(x => x.DeviceName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.DeviceName));

        RuleFor(x => x.Browser)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Browser));

        RuleFor(x => x.OperatingSystem)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.OperatingSystem));

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .WithMessage("IP adresi alınamadı.")
            .MaximumLength(45);
    }
}