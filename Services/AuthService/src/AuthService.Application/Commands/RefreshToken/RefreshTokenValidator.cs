using FluentValidation;

namespace AuthService.Application.Commands.RefreshToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token zorunludur.")
            .MaximumLength(512);

        RuleFor(x => x.IpAddress)
            .NotEmpty()
            .WithMessage("IP adresi alınamadı.")
            .MaximumLength(45);
    }
}
