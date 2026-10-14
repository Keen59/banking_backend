using FluentValidation;

namespace OpenBankingService.Application.Commands.InitiatePayment;

public class InitiatePaymentCommandValidator : AbstractValidator<InitiatePaymentCommand>
{
    public InitiatePaymentCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty().Length(32);
        RuleFor(x => x.SourceAccountId).NotEmpty();
        RuleFor(x => x.DestinationAccountId).NotEmpty()
            .NotEqual(x => x.SourceAccountId);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Description).MaximumLength(256);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
