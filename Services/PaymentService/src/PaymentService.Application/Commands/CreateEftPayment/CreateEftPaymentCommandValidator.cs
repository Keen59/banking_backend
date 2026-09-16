using FluentValidation;

namespace PaymentService.Application.Commands.CreateEftPayment;

public class CreateEftPaymentCommandValidator : AbstractValidator<CreateEftPaymentCommand>
{
    public CreateEftPaymentCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SourceAccountId).NotEmpty();
        RuleFor(x => x.DestinationIban).NotEmpty().MaximumLength(26);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(256);
    }
}
