using CustomerService.Application.Helpers;
using FluentValidation;

namespace CustomerService.Application.Commands.CreateCustomer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Ad zorunludur.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Soyad zorunludur.")
            .MaximumLength(100);

        RuleFor(x => x.NationalId)
            .NotEmpty()
            .WithMessage("TCKN zorunludur.")
            .Must(NationalIdHelper.IsValidTckn)
            .WithMessage("Geçerli bir TCKN giriniz.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)))
            .WithMessage("Müşteri 18 yaşından büyük olmalıdır.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("İl zorunludur.")
            .MaximumLength(100);

        RuleFor(x => x.District)
            .NotEmpty()
            .WithMessage("İlçe zorunludur.")
            .MaximumLength(100);

        RuleFor(x => x.Line1)
            .NotEmpty()
            .WithMessage("Adres zorunludur.")
            .MaximumLength(250);

        RuleFor(x => x.KvkkConsent)
            .Equal(true)
            .WithMessage("KVKK onayı zorunludur.");
    }
}
