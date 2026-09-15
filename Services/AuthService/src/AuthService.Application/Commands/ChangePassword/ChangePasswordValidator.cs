using FluentValidation;

namespace AuthService.Application.Commands.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Mevcut şifre zorunludur.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Yeni şifre zorunludur.")
            .MinimumLength(8)
            .WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(128)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Yeni şifre mevcut şifre ile aynı olamaz.");
    }
}
