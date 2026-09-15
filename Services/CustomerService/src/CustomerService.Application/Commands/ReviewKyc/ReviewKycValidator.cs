using FluentValidation;

namespace CustomerService.Application.Commands.ReviewKyc;

public class ReviewKycCommandValidator : AbstractValidator<ReviewKycCommand>
{
    public ReviewKycCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty();

        RuleFor(x => x.RejectReason)
            .NotEmpty()
            .WithMessage("Red gerekçesi zorunludur.")
            .MaximumLength(500)
            .When(x => !x.Approved);
    }
}
