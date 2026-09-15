using FluentValidation;

namespace CustomerService.Application.Commands.SubmitKyc;

public class SubmitKycCommandValidator : AbstractValidator<SubmitKycCommand>
{
    public SubmitKycCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}
