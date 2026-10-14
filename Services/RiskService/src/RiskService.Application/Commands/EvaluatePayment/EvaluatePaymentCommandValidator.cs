using FluentValidation;

namespace RiskService.Application.Commands.EvaluatePayment;

public class EvaluatePaymentCommandValidator : AbstractValidator<EvaluatePaymentCommand>
{
    public EvaluatePaymentCommandValidator()
    {
        RuleFor(x => x.SourceId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(3);
    }
}
