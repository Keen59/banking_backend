using FluentValidation;

namespace PaymentService.Application.Commands.CreateFxConversion;

public class CreateFxConversionCommandValidator : AbstractValidator<CreateFxConversionCommand>
{
    public CreateFxConversionCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SourceAccountId).NotEmpty();
        RuleFor(x => x.DestinationAccountId).NotEmpty();
        RuleFor(x => x.SourceAmount).GreaterThan(0);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(256);
    }
}
