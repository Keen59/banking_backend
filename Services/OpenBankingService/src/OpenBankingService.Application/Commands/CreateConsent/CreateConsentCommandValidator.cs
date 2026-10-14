using FluentValidation;
using OpenBankingService.Application.Helpers;

namespace OpenBankingService.Application.Commands.CreateConsent;

public class CreateConsentCommandValidator : AbstractValidator<CreateConsentCommand>
{
    public CreateConsentCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.TppClientId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Scopes).Must(ConsentAccess.HasAisOrPis)
            .WithMessage("Consent must include AIS and/or PIS.");
        RuleFor(x => x.AccountIds).NotEmpty();
        RuleFor(x => x.ExpiresInDays).InclusiveBetween(1, 90);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
