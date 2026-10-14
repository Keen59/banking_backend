using FluentValidation;

namespace LoanService.Application.Commands.ApplyLoan;

public class ApplyLoanCommandValidator : AbstractValidator<ApplyLoanCommand>
{
    public ApplyLoanCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.TermMonths).InclusiveBetween(3, 12);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
