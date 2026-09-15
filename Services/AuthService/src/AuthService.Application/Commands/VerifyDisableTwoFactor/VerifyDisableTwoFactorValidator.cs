using FluentValidation;

namespace AuthService.Application.Commands.VerifyDisableTwoFactor;

public class VerifyDisableTwoFactorCommandValidator : AbstractValidator<VerifyDisableTwoFactorCommand>
{
    public VerifyDisableTwoFactorCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Doğrulama kodu zorunludur.")
            .Length(6)
            .WithMessage("Doğrulama kodu 6 haneli olmalıdır.")
            .Matches(@"^\d{6}$")
            .WithMessage("Doğrulama kodu yalnızca rakam içermelidir.");
    }
}
