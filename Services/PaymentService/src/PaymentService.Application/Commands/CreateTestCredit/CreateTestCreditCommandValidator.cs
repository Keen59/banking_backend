using FluentValidation;

namespace PaymentService.Application.Commands.CreateTestCredit;

public class CreateTestCreditCommandValidator : AbstractValidator<CreateTestCreditCommand>
{
    public CreateTestCreditCommandValidator()
    {
        RuleFor(x => x.RequestedByCustomerId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
