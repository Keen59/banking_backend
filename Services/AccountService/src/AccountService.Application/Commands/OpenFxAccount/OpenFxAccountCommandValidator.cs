using FluentValidation;

namespace AccountService.Application.Commands.OpenFxAccount;

public class OpenFxAccountCommandValidator : AbstractValidator<OpenFxAccountCommand>
{
    public OpenFxAccountCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}
