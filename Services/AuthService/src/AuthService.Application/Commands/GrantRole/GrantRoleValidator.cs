using Banking.Contracts.Authorization;
using FluentValidation;

namespace AuthService.Application.Commands.GrantRole;

public class GrantRoleCommandValidator : AbstractValidator<GrantRoleCommand>
{
    public GrantRoleCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.RoleName)
            .NotEmpty()
            .Must(name => name is Roles.Customer or Roles.Operations)
            .WithMessage("Yalnızca Customer veya Operations rolü atanabilir.");
    }
}
