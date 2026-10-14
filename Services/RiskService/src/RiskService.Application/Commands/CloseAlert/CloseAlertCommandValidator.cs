using FluentValidation;

namespace RiskService.Application.Commands.CloseAlert;

public class CloseAlertCommandValidator : AbstractValidator<CloseAlertCommand>
{
    public CloseAlertCommandValidator()
    {
        RuleFor(x => x.AlertId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(512);
    }
}
