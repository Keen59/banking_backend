using FluentValidation;

namespace PaymentService.Application.Commands.CreateIncomingFastPayment;

public class CreateIncomingFastPaymentCommandValidator : AbstractValidator<CreateIncomingFastPaymentCommand>
{
    public CreateIncomingFastPaymentCommandValidator()
    {
        RuleFor(x => x.RequestedByCustomerId).NotEmpty();
        RuleFor(x => x.DestinationIban).NotEmpty().MaximumLength(26);
        RuleFor(x => x.SourceIban).MaximumLength(26);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(256);
    }
}
