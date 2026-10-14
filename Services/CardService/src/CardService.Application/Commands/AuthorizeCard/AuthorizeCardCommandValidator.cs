using FluentValidation;

namespace CardService.Application.Commands.AuthorizeCard;

public class AuthorizeCardCommandValidator : AbstractValidator<AuthorizeCardCommand>
{
    public AuthorizeCardCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Merchant).NotEmpty().MaximumLength(64);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(256);
    }
}
