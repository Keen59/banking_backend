using FluentValidation;

namespace AuthService.Application.Commands.VerifyLoginOtp;

public class VerifyLoginOtpCommandValidator : AbstractValidator<VerifyLoginOtpCommand>
{
    public VerifyLoginOtpCommandValidator()
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
